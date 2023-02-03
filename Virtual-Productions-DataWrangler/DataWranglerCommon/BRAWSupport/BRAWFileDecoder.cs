using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BlackmagicRawAPIInterop;

namespace DataWranglerCommon.BRAWSupport
{
	public class BRAWFileDecoder: IDisposable
	{
		[DllImport("BlackmagicRawAPI")]
		static extern IBlackmagicRawFactory CreateBlackmagicRawFactoryInstance();

		internal class CallbackHelper : IBlackmagicRawCallback
		{
			public IBlackmagicRawFrame? Frame;
			public ManualResetEvent CompletionEvent = new ManualResetEvent(false);

			public void ReadComplete(IBlackmagicRawJob a_job, int a_result, IBlackmagicRawFrame a_frame)
			{
				Frame = a_frame;
				CompletionEvent.Set();
			}

			public void DecodeComplete(IBlackmagicRawJob a_job, int a_result)
			{
			}

			public void ProcessComplete(IBlackmagicRawJob a_job, int a_result,
				IBlackmagicRawProcessedImage a_processedImage)
			{
			}

			public void TrimProgress(IBlackmagicRawJob a_job, float a_progress)
			{
			}

			public void TrimComplete(IBlackmagicRawJob a_job, int a_result)
			{
			}

			public void SidecarMetadataParseWarning(IBlackmagicRawClip a_clip, string a_fileName, uint a_lineNumber,
				string a_info)
			{
			}

			public void SidecarMetadataParseError(IBlackmagicRawClip a_clip, string a_fileName, uint a_lineNumber,
				string a_info)
			{
			}

			public void PreparePipelineComplete(IntPtr a_userData, int a_result)
			{
			}
		}

		private readonly IBlackmagicRawFactory m_factory;
		private readonly IBlackmagicRaw m_codec;

		public BRAWFileDecoder()
		{
			m_factory = CreateBlackmagicRawFactoryInstance();
			m_factory.CreateCodec(out m_codec);

			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
			{
				throw new NotImplementedException();
			}
		}

		public void Dispose()
		{
#pragma warning disable CA1416 //Call site reachable on all platforms. Constructor throws NotImplementedException when platform is not Win32NT
			Marshal.ReleaseComObject(m_factory);
			Marshal.ReleaseComObject(m_codec);
#pragma warning restore CA1416
		}

		public BrawFileMetadata GetMetaForFile(FileInfo a_file)
		{
			CallbackHelper handler = new CallbackHelper();
			m_codec.OpenClip(a_file.FullName, out IBlackmagicRawClip clip);

			m_codec.SetCallback(handler);
			SetupFrameDecodeJob(0, clip);

			clip.GetMetadata("camera_number", out object cameraNumber);
			string cameraNumberString = (string)cameraNumber;
			clip.GetMetadata("date_recorded", out object dateRecordedMeta);
			DateTime dateRecorded = ParseBlackmagicDate((string)dateRecordedMeta);

			handler.CompletionEvent.WaitOne();

			if (handler.Frame == null)
			{
				throw new Exception();
			}

			handler.Frame.GetTimecode(out string timeCode);

			return new BrawFileMetadata(TimeCode.FromString(timeCode), cameraNumberString, dateRecorded);
		}

		private static readonly Regex BlackmagicDateRegex = new Regex("([0-9]{4}):([0-9]{2}):([0-9]{2})");

		private DateTime ParseBlackmagicDate(string a_dateRecorded)
		{
			Match match = BlackmagicDateRegex.Match(a_dateRecorded);
			if (!match.Success)
			{
				throw new FormatException($"Failed to parse blackmagic date from value {a_dateRecorded}");
			}

			int year = int.Parse(match.Groups[1].ValueSpan);
			int month = int.Parse(match.Groups[2].ValueSpan);
			int day = int.Parse(match.Groups[3].ValueSpan);
			return new DateTime(year, month, day);
		}

		public TimeCode GetTimeCodeFromFile(FileInfo a_file, ulong a_frameNumber = 0)
		{
			CallbackHelper handler = new CallbackHelper();
			m_codec.OpenClip(a_file.FullName, out IBlackmagicRawClip clip);
			clip.GetFrameCount(out ulong frameCount);
			if (a_frameNumber > frameCount)
			{
				throw new ArgumentOutOfRangeException(
					$"Tried getting data for frame {a_frameNumber} where only {frameCount} frames exist in the file");
			}

			m_codec.SetCallback(handler);
			SetupFrameDecodeJob(a_frameNumber, clip);

			clip.GetMetadataIterator(out IBlackmagicRawMetadataIterator metaIterator);

			handler.CompletionEvent.WaitOne();
			
			if (handler.Frame == null)
			{
				throw new Exception();
			}

			handler.Frame.GetTimecode(out string timeCode);

			return TimeCode.FromString(timeCode);
		}

		private static void SetupFrameDecodeJob(ulong a_frameNumber, IBlackmagicRawClip clip)
		{
			clip.CreateJobReadFrame(a_frameNumber, out IBlackmagicRawJob readJob);
			readJob.Submit();
		}
	}
}
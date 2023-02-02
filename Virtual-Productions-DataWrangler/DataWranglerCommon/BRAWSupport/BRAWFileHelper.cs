using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using BlackmagicRawAPIInterop;

namespace DataWranglerCommon.BRAWSupport
{
	public class BRAWFileHelper
	{
		private const string BrawDLL = "BlackmagicRawAPI.dll";

		static BRAWFileHelper()
		{
			//FileInfo dllFile = new FileInfo(BrawDLL);
			//if (!dllFile.Exists)
			//{
			//	throw new FileNotFoundException(
			//		$"Missing BRAW API. Please verify that the {BrawDLL} exists next to the executable.");
			//}

			//Assembly loadedBrawApi = Assembly.LoadFile(dllFile.FullName);
			//if (loadedBrawApi == null)
			//{
			//	throw new FileNotFoundException(
			//		$"Missing BRAW API. Please verify that the {BrawDLL} exists next to the executable.");
			//}
		}

		internal class CallbackHelper : IBlackmagicRawCallback
		{
			public IBlackmagicRawFrame? Frame;

			public void ReadComplete(IBlackmagicRawJob a_job, int a_result, IBlackmagicRawFrame a_frame)
			{
				Frame = a_frame;
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

		public static TimeCode GetTimeCodeFromFile(FileInfo a_file, ulong a_frameNumber = 0)
		{
			CallbackHelper handler = new CallbackHelper();
			IBlackmagicRawFactory factory = new CBlackmagicRawFactoryClass();
			factory.CreateCodec(out IBlackmagicRaw codec);
			codec.OpenClip(a_file.FullName, out IBlackmagicRawClip clip);
			codec.SetCallback(handler);
			clip.CreateJobReadFrame(a_frameNumber, out IBlackmagicRawJob readJob);
			readJob.Submit();
			codec.FlushJobs();

			if (handler.Frame == null)
			{
				throw new Exception();
			}

			handler.Frame.GetTimecode(out string timeCode);

			return TimeCode.FromString(timeCode);
		}
	}
}
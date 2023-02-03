using System.Reflection;
using System.Runtime.InteropServices;
using BlackmagicRawAPIInterop;

namespace DataWranglerCommon.BRAWSupport
{
	public class BRAWFileHelper
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		static extern bool FreeLibrary(IntPtr hModule);

		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		static extern bool LoadLibraryA(string hModule);

		[DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

		[DllImport("kernel32.dll")]
		static extern bool GetModuleHandleExA(int dwFlags, string ModuleName, ref IntPtr phModule);

		[DllImport("BlackmagicRawAPI")]
		static extern IBlackmagicRawFactory CreateBlackmagicRawFactoryInstance();

		private const string BrawDLL = "BlackmagicRawAPI.dll";

		static BRAWFileHelper()
		{
			FileInfo dllFile = new FileInfo(BrawDLL);
			if (!dllFile.Exists)
			{
				throw new FileNotFoundException(
					$"Missing BRAW API. Please verify that the {BrawDLL} exists next to the executable.");
			}

			bool success = LoadLibraryA(dllFile.FullName);
			if (!success)
			{
				throw new FileNotFoundException(
					$"Missing BRAW API. Please verify that the {BrawDLL} exists next to the executable.");
			}

			IntPtr modulePtr = IntPtr.Zero;
			if (!GetModuleHandleExA(0, "BlackmagicRawAPI", ref modulePtr))
			{
				throw new NotImplementedException();
			}

			//YourFunctionDelegate function = Marshal.GetDelegateForFunctionPointer(funcaddr, typeof(YourFunctionDelegate)) as YourFunctionDelegate;
			//function.Invoke(pass here your parameters);

			GetProcAddress(modulePtr, "CreateBlackmagicRawFactoryInstance");
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
			IBlackmagicRawFactory factory = CreateBlackmagicRawFactoryInstance();
			factory.CreateCodec(out IBlackmagicRaw codec);
			codec.OpenClip(a_file.FullName, out IBlackmagicRawClip clip);
			codec.SetCallback(handler);
			SetupFrameDecodeJob(a_frameNumber, clip);
			
			codec.FlushJobs();

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
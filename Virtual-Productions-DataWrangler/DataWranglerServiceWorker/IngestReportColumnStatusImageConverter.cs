using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DataWranglerServiceWorker
{
	internal class IngestReportColumnStatusImageConverter: IValueConverter
	{
		private readonly BitmapImage SuccessImage = new BitmapImage(new Uri("pack://application:,,,/DataWranglerCommonWPF;component/Resources/StatusIcons/Success.png"));
		private readonly BitmapImage WarningImage = new BitmapImage(new Uri("pack://application:,,,/DataWranglerCommonWPF;component/Resources/StatusIcons/Warning.png"));
		private readonly BitmapImage ErrorImage = new BitmapImage(new Uri("pack://application:,,,/DataWranglerCommonWPF;component/Resources/StatusIcons/Error.png"));
		private readonly BitmapImage PendingImage = new BitmapImage(new Uri("pack://application:,,,/DataWranglerCommonWPF;component/Resources/StatusIcons/Pending.png"));
		private readonly BitmapImage InformationalImage = new BitmapImage(new Uri("pack://application:,,,/DataWranglerCommonWPF;component/Resources/StatusIcons/Informational.png"));

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			IngestFileReportEntry.EStatusImageType val = (IngestFileReportEntry.EStatusImageType) value;
			switch (val)
			{
				case IngestFileReportEntry.EStatusImageType.Error:
					return ErrorImage;
				case IngestFileReportEntry.EStatusImageType.Warning:
					return WarningImage;
				case IngestFileReportEntry.EStatusImageType.Success:
					return SuccessImage;
				case IngestFileReportEntry.EStatusImageType.Pending:
					return PendingImage;
				case IngestFileReportEntry.EStatusImageType.Informational:
					return InformationalImage;
			}

			throw new ArgumentException($"Unknown value {val} for StatusImageType");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

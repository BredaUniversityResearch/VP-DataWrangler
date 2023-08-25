using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DataWranglerServiceWorker
{
	internal class IngestReportColumnStatusImageConverter: IValueConverter
	{
		private BitmapImage UpToDateImage = new BitmapImage(new Uri("pack://application:,,,/DataWranglerCommonWPF;component/Resources/StatusIcons/Pending.png"));
		private BitmapImage ErrorImage = new BitmapImage(new Uri("pack://application:,,,/DataWranglerCommonWPF;component/Resources/StatusIcons/Error.png"));

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string val = (string) value;
			if (val == DataImportWorker.ECopyResult.Success.ToString())
			{
				return UpToDateImage;
			}
			else if (val == DataImportWorker.ECopyResult.FileAlreadyUpToDate.ToString())
			{
				return UpToDateImage;
			}
			else
			{
				return ErrorImage;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

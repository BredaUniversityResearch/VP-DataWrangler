﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DataWranglerCommonWPF
{
	public partial class AsyncOperationChangeFeedback : Canvas
	{
		private FrameworkElement? m_targetChildElement = null;

		public AsyncOperationChangeFeedback()
		{
			InitializeComponent();

			LoadingSpinnerInstance.SetIsLoading(false);
		}

		protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
		{
			base.OnVisualChildrenChanged(visualAdded, visualRemoved);

			if (VisualChildrenCount == 2)
			{
				m_targetChildElement = (FrameworkElement)Children[1];
				if (m_targetChildElement == null)
				{
					throw new Exception("No content for adorner");
				}
				SetZIndex(LoadingSpinnerInstance, 100);
			}

		}

		protected override Size ArrangeOverride(Size arrangeSize)
		{
			if (m_targetChildElement != null)
			{	
				Rect targetRect = new Rect(new Point(0, 0), new Size(arrangeSize.Width, arrangeSize.Height));
				LoadingSpinnerInstance.Arrange(targetRect);
				LoadingSpinnerInstance.Width = arrangeSize.Width;
				LoadingSpinnerInstance.Height = arrangeSize.Height;

				//LoadingSpinnerInstance.Invalidate();
				m_targetChildElement.Arrange(targetRect);
			}

			return arrangeSize;
		}

		protected override Size MeasureOverride(Size constraint)
		{
			base.MeasureOverride(constraint);

			if (m_targetChildElement == null)
			{
				return new Size();
			}

			double width = m_targetChildElement.DesiredSize.Width;
			if ((HorizontalAlignment == HorizontalAlignment.Stretch ||
			    width <= 0.0) &&
			    constraint.Width < double.PositiveInfinity)
			{
				width = constraint.Width;
			}

			double height = m_targetChildElement.DesiredSize.Height;
			if (height <= 0.0f &&
			    constraint.Height < double.PositiveInfinity)
			{
				height = DesiredSize.Height;
			}

			Size result = new Size(width, height);

			m_targetChildElement.Measure(result);

			return result;
		}

		public static AsyncOperationChangeFeedback? FindFeedbackElementFrom(FrameworkElement a_element)
		{
			return FindAncestor<AsyncOperationChangeFeedback>(a_element);
		}

		private static T? FindAncestor<T>(DependencyObject a_targetElement)
			where T : DependencyObject
		{
			DependencyObject? currentElement = a_targetElement;
			do
			{
				currentElement = VisualTreeHelper.GetParent(currentElement);
				if (currentElement is T typedResult)
					return typedResult;
			}
			while (currentElement != null);

			return null;
		}

		public void ProvideFeedback(Task a_asyncRunningTask)
		{
			Dispatcher.InvokeAsync(() => { LoadingSpinnerInstance.SetIsLoading(true); });
			a_asyncRunningTask.ContinueWith((_) => Dispatcher.InvokeAsync(() => LoadingSpinnerInstance.SetIsLoading(false)));

		}
	}
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace MusicMessage.UserCtrls
{
	public enum ImageCropMode
	{
		Free,             // Свободное соотношение
		FixedAspectRatio  // Фиксированное соотношение сторон
	}
	public partial class ImageCropperControl : UserControl
	{
		public event Action<string> OnImageCropped;
		public event Action OnCancelled;

		private string _originalImagePath;
		private double _scale = 1.0;


		private System.Windows.Point _startPoint;
		private Vector _startOffset;
		private bool _isDraggingImage = false;
		private bool _isDraggingCrop = false;
		private bool _isResizingCrop = false;
		private string _resizeDirection = "";
		private System.Windows.Size _startSize;


		private double _cropX = 0;
		private double _cropY = 0;
		public static readonly DependencyProperty CropModeProperty =
		   DependencyProperty.Register(
			   nameof(CropMode),
			   typeof(ImageCropMode),
			   typeof(ImageCropperControl),
			   new PropertyMetadata(ImageCropMode.Free, OnCropModeChanged));

		public static readonly DependencyProperty TargetAspectRatioProperty =
			DependencyProperty.Register(
				nameof(TargetAspectRatio),
				typeof(double),
				typeof(ImageCropperControl),
				new PropertyMetadata(3.0, OnTargetAspectRatioChanged));
		public ImageCropperControl()
		{
			InitializeComponent();
			InitializeEvents();
			UpdateCropBorderFromProperties();
		}
		public ImageCropMode CropMode
		{
			get => (ImageCropMode)GetValue(CropModeProperty);
			set => SetValue(CropModeProperty, value);
		}

		public double TargetAspectRatio
		{
			get => (double)GetValue(TargetAspectRatioProperty);
			set => SetValue(TargetAspectRatioProperty, value);
		}

		// Обработчики изменения свойств
		private static void OnCropModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is ImageCropperControl control)
			{
				control.UpdateCropBorderFromProperties();
				control.UpdateAspectHintText();
			}
		}

		private static void OnTargetAspectRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is ImageCropperControl control)
			{
				if (control.CropMode == ImageCropMode.FixedAspectRatio)
				{
					control.UpdateCropBorderFromProperties();
				}
				control.UpdateAspectHintText();
			}
		}

		private void UpdateAspectHintText()
		{
			// Убедитесь, что TbAspectHint существует в XAML
			if (TbAspectHint != null)
			{
				if (CropMode == ImageCropMode.FixedAspectRatio)
				{
					TbAspectHint.Text = $"⬜ Соотношение: {TargetAspectRatio:0.##} (Ш:Г)";
				}
				else
				{
					TbAspectHint.Text = "⬜ Соотношение: Свободное";
				}
			}
		}

		private void UpdateCropBorderFromProperties()
		{
			if (CropBorder == null) return;

			if (CropMode == ImageCropMode.FixedAspectRatio)
			{
				double initialWidth = 400;
				double initialHeight = initialWidth / TargetAspectRatio;

				if (initialHeight < 100)
				{
					initialHeight = 100;
					initialWidth = initialHeight * TargetAspectRatio;
				}

				CropBorder.Width = initialWidth;
				CropBorder.Height = initialHeight;
			}
			else
			{
				CropBorder.Width = 300;
				CropBorder.Height = 200;
			}

			var ib = GetImageBounds();
			_cropX = ib.Left + (ib.Width - CropBorder.Width) / 2;
			_cropY = ib.Top + (ib.Height - CropBorder.Height) / 2;
			Canvas.SetLeft(CropBorder, _cropX);
			Canvas.SetTop(CropBorder, _cropY);
		}
		private void InitializeEvents()
		{

			SourceImage.MouseLeftButtonDown += Image_MouseLeftButtonDown;
			SourceImage.MouseMove += Image_MouseMove;
			SourceImage.MouseLeftButtonUp += Image_MouseLeftButtonUp;
			SourceImage.MouseWheel += Image_MouseWheel;

			AddResizeHandlers(ResizeNW, "NW");
			AddResizeHandlers(ResizeNE, "NE");
			AddResizeHandlers(ResizeSW, "SW");
			AddResizeHandlers(ResizeSE, "SE");
			AddResizeHandlers(ResizeW, "W");
			AddResizeHandlers(ResizeE, "E");
			AddResizeHandlers(ResizeN, "N");
			AddResizeHandlers(ResizeS, "S");

			MainContainer.SizeChanged += MainContainer_SizeChanged;
		}

		private void AddResizeHandlers(FrameworkElement element, string direction)
		{
			element.MouseLeftButtonDown += (s, e) => { e.Handled = true; StartResize(direction, e); };
			element.MouseMove += (s, e) => { if (_isResizingCrop) HandleResize(e); };
			element.MouseLeftButtonUp += (s, e) => StopResize();
		}

		private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{

			var posMain = e.GetPosition(MainContainer);
			if (IsMouseOverCropArea(posMain)) return;

			_isDraggingImage = true;
			SourceImage.CaptureMouse();
			_startPoint = e.GetPosition(MainContainer);
			_startOffset = new Vector(ImageTranslate.X, ImageTranslate.Y);
		}

		private void Image_MouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				if (_isDraggingImage)
				{
					Vector offset = e.GetPosition(MainContainer) - _startPoint;
					ImageTranslate.X = _startOffset.X + offset.X;
					ImageTranslate.Y = _startOffset.Y + offset.Y;

					UpdateCropBounds();
				}

				if (IsMouseOverCropArea(e.GetPosition(MainContainer)))
				{
					var pos = e.GetPosition(CropBorder);
					UpdateCursor(pos);
				}
			}
			catch { }
		}

		private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (_isDraggingImage)
			{
				_isDraggingImage = false;
				SourceImage.ReleaseMouseCapture();
			}
		}

		private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
			_scale = Math.Max(0.1, Math.Min(5.0, _scale * zoomFactor));
			ImageScale.ScaleX = ImageScale.ScaleY = _scale;
			UpdateCropBounds();
		}

		private void CropBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var pos = e.GetPosition(CropBorder);
			var resizeDir = GetResizeDirection(pos);

			if (!string.IsNullOrEmpty(resizeDir))
			{
				StartResize(resizeDir, e);
			}
			else
			{
				StartDragCrop(e);
			}
		}

		private void CropBorder_MouseMove(object sender, MouseEventArgs e)
		{
			if (_isResizingCrop)
			{
				HandleResize(e);
			}
			else if (_isDraggingCrop)
			{
				HandleDragCrop(e);
			}
			else
			{
				var pos = e.GetPosition(CropBorder);
				UpdateCursor(pos);
			}
		}

		private void CropBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (_isResizingCrop) StopResize();
			if (_isDraggingCrop) StopDragCrop();
		}

		private void StartDragCrop(MouseButtonEventArgs e)
		{
			_isDraggingCrop = true;
			CropBorder.CaptureMouse();
			_startPoint = e.GetPosition(MainContainer);
			_startOffset = new Vector(_cropX, _cropY);
		}

		private void HandleDragCrop(MouseEventArgs e)
		{
			var currentPos = e.GetPosition(MainContainer);
			double deltaX = currentPos.X - _startPoint.X;
			double deltaY = currentPos.Y - _startPoint.Y;

			double translateX = _startOffset.X + deltaX;
			double translateY = _startOffset.Y + deltaY;

			var ib = GetImageBounds();
			translateX = Math.Max(ib.Left, Math.Min(translateX, ib.Right - CropBorder.ActualWidth));
			translateY = Math.Max(ib.Top, Math.Min(translateY, ib.Bottom - CropBorder.ActualHeight));

			_cropX = translateX;
			_cropY = translateY;
			Canvas.SetLeft(CropBorder, _cropX);
			Canvas.SetTop(CropBorder, _cropY);
		}

		private void StopDragCrop()
		{
			_isDraggingCrop = false;
			CropBorder.ReleaseMouseCapture();
		}

		private void StartResize(string direction, MouseButtonEventArgs e)
		{
			_isResizingCrop = true;
			_resizeDirection = direction;
			CropBorder.CaptureMouse();
			_startPoint = e.GetPosition(MainContainer);
			_startOffset = new Vector(_cropX, _cropY);
			_startSize = new System.Windows.Size(CropBorder.ActualWidth, CropBorder.ActualHeight);
			e.Handled = true;
		}

		private void HandleResize(MouseEventArgs e)
		{
			if (!_isResizingCrop) return;

			var currentPos = e.GetPosition(MainContainer);
			double deltaX = currentPos.X - _startPoint.X;
			double deltaY = currentPos.Y - _startPoint.Y;

			double startX = _startOffset.X;
			double startY = _startOffset.Y;
			double startWidth = _startSize.Width;
			double startHeight = _startSize.Height;

			double newX = startX;
			double newY = startY;
			double newWidth = startWidth;
			double newHeight = startHeight;

			switch (_resizeDirection)
			{
				case "NW":
					newX = startX + deltaX;
					newY = startY + deltaY;
					newWidth = startWidth - deltaX;
					newHeight = startHeight - deltaY;
					break;
				case "NE":
					newY = startY + deltaY;
					newWidth = startWidth + deltaX;
					newHeight = startHeight - deltaY;
					break;
				case "SW":
					newX = startX + deltaX;
					newWidth = startWidth - deltaX;
					newHeight = startHeight + deltaY;
					break;
				case "SE":
					newWidth = startWidth + deltaX;
					newHeight = startHeight + deltaY;
					break;
				case "W":
					newX = startX + deltaX;
					newWidth = startWidth - deltaX;
					break;
				case "E":
					newWidth = startWidth + deltaX;
					break;
				case "N":
					newY = startY + deltaY;
					newHeight = startHeight - deltaY;
					break;
				case "S":
					newHeight = startHeight + deltaY;
					break;
			}

			const double MIN_W = 50;
			const double MIN_H = 33;
			newWidth = Math.Max(MIN_W, newWidth);
			newHeight = Math.Max(MIN_H, newHeight);

			if (CropMode == ImageCropMode.FixedAspectRatio &&
				(_resizeDirection == "NW" || _resizeDirection == "NE" ||
				 _resizeDirection == "SW" || _resizeDirection == "SE"))
			{
				// Вместо старой логики с жестким 3.0 используем свойство TargetAspectRatio
				if (Math.Abs(deltaX) > Math.Abs(deltaY))
				{
					newHeight = newWidth / TargetAspectRatio;
				}
				else
				{
					newWidth = newHeight * TargetAspectRatio;
				}
			}


			var ib = GetImageBounds();

			if (newX < ib.Left)
			{
				newWidth -= (ib.Left - newX);
				newX = ib.Left;
			}
			if (newY < ib.Top)
			{
				newHeight -= (ib.Top - newY);
				newY = ib.Top;
			}

			if (newX + newWidth > ib.Right) newWidth = ib.Right - newX;
			if (newY + newHeight > ib.Bottom) newHeight = ib.Bottom - newY;

			_cropX = newX;
			_cropY = newY;
			CropBorder.Width = newWidth;
			CropBorder.Height = newHeight;
			Canvas.SetLeft(CropBorder, _cropX);
			Canvas.SetTop(CropBorder, _cropY);

			_startPoint = currentPos;
			_startOffset = new Vector(_cropX, _cropY);
			_startSize = new System.Windows.Size(CropBorder.Width, CropBorder.Height);
		}

		private void StopResize()
		{
			_isResizingCrop = false;
			CropBorder.ReleaseMouseCapture();
		}

		private string GetResizeDirection(System.Windows.Point pos)
		{
			double border = 10;
			double width = CropBorder.ActualWidth;
			double height = CropBorder.ActualHeight;

			if (pos.X < border && pos.Y < border) return "NW";
			if (pos.X > width - border && pos.Y < border) return "NE";
			if (pos.X < border && pos.Y > height - border) return "SW";
			if (pos.X > width - border && pos.Y > height - border) return "SE";
			if (pos.X < border) return "W";
			if (pos.X > width - border) return "E";
			if (pos.Y < border) return "N";
			if (pos.Y > height - border) return "S";

			return "";
		}

		private void UpdateCursor(System.Windows.Point pos)
		{
			var direction = GetResizeDirection(pos);
			CropBorder.Cursor = string.IsNullOrEmpty(direction) ? Cursors.SizeAll : GetDirectionCursor(direction);
		}

		private Cursor GetDirectionCursor(string direction)
		{
			return direction switch
			{
				"NW" or "SE" => Cursors.SizeNWSE,
				"NE" or "SW" => Cursors.SizeNESW,
				"W" or "E" => Cursors.SizeWE,
				"N" or "S" => Cursors.SizeNS,
				_ => Cursors.Arrow
			};
		}

		private bool IsMouseOverCropArea(System.Windows.Point pos)
		{
			return pos.X >= _cropX && pos.X <= _cropX + CropBorder.ActualWidth &&
				   pos.Y >= _cropY && pos.Y <= _cropY + CropBorder.ActualHeight;
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (string.IsNullOrEmpty(_originalImagePath))
				{
					MessageBox.Show("Изображение не загружено");
					return;
				}

				var croppedPath = CropImage();
				OnImageCropped?.Invoke(croppedPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка обрезки: {ex.Message}");
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			OnCancelled?.Invoke();
		}

		private string CropImage()
		{
			using (var sourceImage = System.Drawing.Image.FromFile(_originalImagePath))
			{
				var cropArea = GetCropAreaInImageCoordinates(sourceImage);

				using (var croppedImage = new Bitmap((int)cropArea.Width, (int)cropArea.Height))
				using (var graphics = Graphics.FromImage(croppedImage))
				{
					graphics.DrawImage(sourceImage, new RectangleF(0, 0, cropArea.Width, cropArea.Height),
									 cropArea, GraphicsUnit.Pixel);

					var croppedPath = Path.Combine(
						Path.GetDirectoryName(_originalImagePath),
						$"cropped_cover_{DateTime.Now:yyyyMMddHHmmss}.jpg"
					);

					croppedImage.Save(croppedPath, ImageFormat.Jpeg);
					return croppedPath;
				}
			}
		}

		private RectangleF GetCropAreaInImageCoordinates(System.Drawing.Image sourceImage)
		{

			var ib = GetImageBounds();


			double cropLeftOnImageDisplay = _cropX - ib.Left;
			double cropTopOnImageDisplay = _cropY - ib.Top;

			if (!(SourceImage.Source is BitmapSource bs))
			{
				return new RectangleF(0, 0, 1, 1);
			}

			double scaleX = bs.PixelWidth / ib.Width;
			double scaleY = bs.PixelHeight / ib.Height;

			double cropX = cropLeftOnImageDisplay * scaleX;
			double cropY = cropTopOnImageDisplay * scaleY;
			double cropW = CropBorder.Width * scaleX;
			double cropH = CropBorder.Height * scaleY;


			cropX = Math.Max(0, Math.Min(cropX, sourceImage.Width - cropW));
			cropY = Math.Max(0, Math.Min(cropY, sourceImage.Height - cropH));
			cropW = Math.Min(cropW, sourceImage.Width - cropX);
			cropH = Math.Min(cropH, sourceImage.Height - cropY);

			return new RectangleF((float)cropX, (float)cropY, (float)cropW, (float)cropH);
		}

		private Rect GetImageBounds()
		{

			if (!(SourceImage.Source is BitmapSource bs) || MainContainer.ActualWidth <= 0 || MainContainer.ActualHeight <= 0)
			{
				return new Rect(0, 0, MainContainer.ActualWidth, MainContainer.ActualHeight);
			}

			double imgPixelW = bs.PixelWidth;
			double imgPixelH = bs.PixelHeight;


			double fit = Math.Min(MainContainer.ActualWidth / imgPixelW, MainContainer.ActualHeight / imgPixelH);

			double dispW = imgPixelW * fit * _scale;
			double dispH = imgPixelH * fit * _scale;

			double left = (MainContainer.ActualWidth - dispW) / 2 + ImageTranslate.X;
			double top = (MainContainer.ActualHeight - dispH) / 2 + ImageTranslate.Y;

			return new Rect(left, top, dispW, dispH);
		}

	

		public void LoadImage(string imagePath)
		{
			try
			{
				_originalImagePath = imagePath;

				if (!File.Exists(imagePath))
				{
					MessageBox.Show("Файл не найден");
					return;
				}

				SourceImage.Source = null;

				var bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.UriSource = new Uri(imagePath);
				bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.EndInit();
				bitmap.Freeze();

				SourceImage.Source = bitmap;

				SourceImage.Loaded += (s, e) =>
				{
					Dispatcher.BeginInvoke(new Action(() =>
					{
						ResetView();
					}), System.Windows.Threading.DispatcherPriority.Loaded);
				};
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки: {ex.Message}");
			}
		}

		private void ResetView()
		{
			_scale = 1.0;
			ImageScale.ScaleX = ImageScale.ScaleY = _scale;
			ImageTranslate.X = ImageTranslate.Y = 0;

			var ib = GetImageBounds();

			// Используем логику в зависимости от режима
			double w, h;
			if (CropMode == ImageCropMode.FixedAspectRatio)
			{
				w = Math.Min(400, ib.Width);
				h = w / TargetAspectRatio;

				// Для аватара (квадрат) убедимся, что размер адекватный
				if (TargetAspectRatio == 1.0)
				{
					w = h = Math.Min(300, Math.Min(ib.Width, ib.Height));
				}
			}
			else
			{
				w = Math.Min(300, ib.Width);
				h = Math.Min(200, ib.Height);
			}

			_cropX = ib.Left + (ib.Width - w) / 2;
			_cropY = ib.Top + (ib.Height - h) / 2;

			CropBorder.Width = w;
			CropBorder.Height = h;
			Canvas.SetLeft(CropBorder, _cropX);
			Canvas.SetTop(CropBorder, _cropY);
		}

		private void UpdateCropBounds()
		{

			var ib = GetImageBounds();


			if (ib.Width <= 0 || ib.Height <= 0) return;

			if (CropBorder.Width > ib.Width) CropBorder.Width = ib.Width;
			if (CropBorder.Height > ib.Height) CropBorder.Height = ib.Height;

			_cropX = Math.Max(ib.Left, Math.Min(_cropX, ib.Right - CropBorder.Width));
			_cropY = Math.Max(ib.Top, Math.Min(_cropY, ib.Bottom - CropBorder.Height));

			Canvas.SetLeft(CropBorder, _cropX);
			Canvas.SetTop(CropBorder, _cropY);
		}

		private void MainContainer_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateCropBounds();
		}

		
	}
}

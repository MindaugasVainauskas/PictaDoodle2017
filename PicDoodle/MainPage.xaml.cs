using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PicDoodle
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            //load the inktoolbar with all the tools
            itbToolBar.Loaded += itbToolBar_Loaded;

            //load the inkCanvas and set the allowed input devices
            icvCanvas.Loaded += IcvCanvas_Loaded;
            grdImageCanvas.SizeChanged += GrdImageCanvas_SizeChanged;
           
        }

        private void GrdImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            icvCanvas.Width = Window.Current.Bounds.Width;
            imgPicture.Width = Window.Current.Bounds.Width;
        }

        //method to load inkcanvas supported input devices
        private void IcvCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            icvCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            
        }

        //load up inktoolbar and start off with the pen as active tool in it.
        private void itbToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            InkToolbarToolButton penButton = itbToolBar.GetToolButton(InkToolbarTool.BallpointPen);
            itbToolBar.ActiveTool = penButton;            
        }

        //event handler for making new canvas(Essentially I'm just resetting the inkCanvas to clear state)
        private void New_Click(object sender, RoutedEventArgs e)
        {            
            //set canvas strokes to 0
            icvCanvas.InkPresenter.StrokeContainer.Clear();            
            
            //set image source to null to wipe current image
            imgPicture.Source = null;

        }

        //event handler to open camera and take a picture
        private async void Photo_Click(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI photoCapture = new CameraCaptureUI();
            photoCapture.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Png;

            StorageFile photoStored = await photoCapture.CaptureFileAsync(CameraCaptureUIMode.Photo);

            //picture capture cancelled
            if (photoStored == null)
            {
                return;
            }
            else
            {
                //add image at runtime
                using (IRandomAccessStream imageStream = await photoStored.OpenAsync(FileAccessMode.Read))
                {
                    //make new bitmap image
                    BitmapImage cvBitmapImage = new BitmapImage();

                    //set bitmapimage source to file stream
                    cvBitmapImage.SetSource(imageStream);
                    //set image source to bitmap image
                    imgPicture.Source = cvBitmapImage;

                }
            }

            
        }

        //event handler to pick picture from gallery
        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            //open file picker
            var imagePicker = new FileOpenPicker();

            imagePicker.ViewMode = PickerViewMode.Thumbnail; //list should be given in thumbnail as opposed to list
            imagePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary; //Starting folder should be pictures library
            //following are file types permitted to select
            imagePicker.FileTypeFilter.Add(".jpg");
            imagePicker.FileTypeFilter.Add(".jpeg");
            imagePicker.FileTypeFilter.Add(".bmp");
            imagePicker.FileTypeFilter.Add(".png");

            //wait for an image to be selected
            var image = await imagePicker.PickSingleFileAsync();

            if (image != null)
            {                              
                //add image at runtime
                using (IRandomAccessStream imageStream = await image.OpenAsync(FileAccessMode.Read))
                {
                    //make new bitmap image
                    BitmapImage cvBitmapImage = new BitmapImage();
                   
                    //set bitmapimage source to file stream
                    cvBitmapImage.SetSource(imageStream);
                    //set image source to bitmap image
                    imgPicture.Source = cvBitmapImage;
                    imgPicture.Stretch = Stretch.Fill;
                   
                }
                
               
            }
            else // user cancelled image selection
            {
                return;
            }
        }

        //event handler to save image
        private void Save_Click(object sender, RoutedEventArgs e)
        {

        }

        //event handler to share image
        private void Share_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

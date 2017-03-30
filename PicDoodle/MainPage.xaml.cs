using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.ViewManagement;
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
        //Bitmap image that will be used for pthoto/image pictures to be displayed under inkcanvas
        BitmapImage cvBitmapImage;

        StorageFile imageFile;

        public MainPage()
        {
            this.InitializeComponent();         
            this.Loaded += MainPage_Loaded;
            
            grdImageCanvas.SizeChanged += GrdImageCanvas_SizeChanged;
           
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //here I'm simply settign prefered size of window that application should launch in if on pc.
            ApplicationView.PreferredLaunchViewSize = new Size(400, 650);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
           

            //load the inktoolbar with all the tools
            itbToolBar.Loaded += itbToolBar_Loaded;
            //load the inkCanvas and set the allowed input devices
            icvCanvas.Loaded += IcvCanvas_Loaded;
        }

        //this event will change sizes of ink canvas and image to fit current window size
        private void GrdImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            icvCanvas.Width = Window.Current.Bounds.Width;
            icvCanvas.Height = Window.Current.Bounds.Width * 0.8; 
            //not sure which width setting would be more optimal for image          
            imgPicture.Width = Window.Current.Bounds.Width;
            imgPicture.Height = Window.Current.Bounds.Width * 0.8;           
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

        byte[] _buffer;

        //event handler to open camera and take a picture
        private async void Photo_Click(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI photoCapture = new CameraCaptureUI();
            photoCapture.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Png;           
            photoCapture.PhotoSettings.CroppedAspectRatio = new Size(5, 4);
            photoCapture.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.HighestAvailable;           

            imageFile = await photoCapture.CaptureFileAsync(CameraCaptureUIMode.Photo);
            

            //picture capture cancelled
            if (imageFile == null)
            {
                return;
            }
            else
            {
               
                //get the image stream from camera
                IRandomAccessStream imageStream;
                imageStream = await imageFile.OpenAsync(FileAccessMode.ReadWrite);
                
                //make new bitmap image
                cvBitmapImage = new BitmapImage();

                //set bitmapimage source to file stream
                cvBitmapImage.SetSource(imageStream);
                //set image source to bitmap image
                imgPicture.Source = cvBitmapImage;


                RandomAccessStreamReference rasr = RandomAccessStreamReference.CreateFromStream(imageStream);//.CreateFromUri(cvBitmapImage.UriSource);
                var streamWithContent = await rasr.OpenReadAsync();
                _buffer = new byte[streamWithContent.Size];
                await streamWithContent.ReadAsync(_buffer.AsBuffer(), (uint)streamWithContent.Size, InputStreamOptions.None);


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
            imageFile = await imagePicker.PickSingleFileAsync();

            if (imageFile != null)
            {
               
                //add image at runtime
                using (IRandomAccessStream imageStream = await imageFile.OpenAsync(FileAccessMode.ReadWrite))
                {

                    //make new bitmap image
                    cvBitmapImage = new BitmapImage();
 
                    //set bitmapimage source to file stream
                    cvBitmapImage.SetSource(imageStream);
                    //set image source to bitmap image
                    imgPicture.Source = cvBitmapImage;
                    imgPicture.Stretch = Stretch.Fill;


                    
                    RandomAccessStreamReference rasr = RandomAccessStreamReference.CreateFromStream(imageStream);
                    var streamWithContent = await rasr.OpenReadAsync();
                    _buffer = new byte[streamWithContent.Size];
                    await streamWithContent.ReadAsync(_buffer.AsBuffer(), (uint)streamWithContent.Size, InputStreamOptions.None);

                }


            }
            else // user cancelled image selection
            {
                return;
            }
        }

        //event handler to save image
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            //should merge inkcanvas strokes and underlying image to single image file
            //after merging it will save the resulting combo file to picture gallery

            //Get the picture gallery folder
            StorageFolder imageStorage = KnownFolders.SavedPictures;
            //create file for new image
            var mergedImage = await imageStorage.CreateFileAsync("pictaDoodle_" + DateTime.Now.ToFileTime() + ".png", CreationCollisionOption.ReplaceExisting);

            //create canvasDevice
            CanvasDevice cvDevice = CanvasDevice.GetSharedDevice();
            //Create canvas render target
            CanvasRenderTarget cvrTarget = new CanvasRenderTarget(cvDevice, (int)icvCanvas.Width, (int)(icvCanvas.Width*0.8), 96);
                       
            //start the drawing session
            using (var ds = cvrTarget.CreateDrawingSession())
            {
                //set destination rectangle that will determine the size of the editable image in application
                Rect rectangle = new Rect(0, 0, icvCanvas.Width, (icvCanvas.Width*0.8));
               
                ds.Clear(Colors.White);

                var editableImage = await CanvasBitmap.LoadAsync(cvDevice, imageFile.Path);
                //var editableImage = CanvasBitmap.CreateFromBytes(cvDevice.Device, _buffer,
                //                                                 (int)imgPicture.Width, (int)imgPicture.Height,
                //                                                 Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized);

                //var swBmp = new SoftwareBitmap(BitmapPixelFormat.Bgra8, (int)cvrTarget.SizeInPixels.Width, 
                //                                (int)cvrTarget.SizeInPixels.Height);

                //swBmp.CopyFromBuffer(_buffer.AsBuffer/*());*/
              //  var editableImage = CanvasBitmap.CreateFromSoftwareBitmap(cvDevice.Device, swBmp);

                ds.DrawImage(editableImage, rectangle);
                ds.DrawInk(icvCanvas.InkPresenter.StrokeContainer.GetStrokes());
                }

                using (var fStream = await mergedImage.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await cvrTarget.SaveAsync(fStream, CanvasBitmapFileFormat.Png, 1f);
                }                
            }
    

            

        
        //event handler to share image
        private void Share_Click(object sender, RoutedEventArgs e)
        {
            //will implement sharing function later
        }

        
    }
}

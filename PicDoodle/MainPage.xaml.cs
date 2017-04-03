using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
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
                
                imageStream = await imageFile.OpenAsync(FileAccessMode.Read);
                
                //make new bitmap image
                cvBitmapImage = new BitmapImage();

                //set bitmapimage source to file stream
                cvBitmapImage.SetSource(imageStream);
                //set image source to bitmap image
                imgPicture.Source = cvBitmapImage;

                //create stream reference from image stream(From camera capture)
                RandomAccessStreamReference rasr = RandomAccessStreamReference.CreateFromStream(imageStream);
                var streamWithContent = await rasr.OpenReadAsync();
                //create the buffer from the image stream
                _buffer = new byte[streamWithContent.Size];
                //fill the buffer
                await streamWithContent.ReadAsync(_buffer.AsBuffer(), (uint)streamWithContent.Size, InputStreamOptions.None);

                //open up local application folder
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                //create temporary image in local application folder. This image will get overwritten every time image is picked
                if (await localFolder.GetFileAsync("tempImage.png") != null)
                {
                    File.Delete("tempImage.png");
                }

                StorageFile localImageFile = await localFolder.CreateFileAsync("tempImage.png", CreationCollisionOption.ReplaceExisting);

                //write out image into file from buffer
                await FileIO.WriteBytesAsync(localImageFile, _buffer);


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
                using (IRandomAccessStream imageStream = await imageFile.OpenAsync(FileAccessMode.Read))
                {

                    //make new bitmap image
                    cvBitmapImage = new BitmapImage();
 
                    //set bitmapimage source to file stream
                    cvBitmapImage.SetSource(imageStream);
                    //set image source to bitmap image
                    imgPicture.Source = cvBitmapImage;
                    imgPicture.Stretch = Stretch.Fill;                    

                    //open random access reference stream from file stream
                    RandomAccessStreamReference rasr = RandomAccessStreamReference.CreateFromStream(imageStream);
                    var streamWithContent = await rasr.OpenReadAsync();
                    //create byte array
                    _buffer = new byte[streamWithContent.Size];
                    //fill the array
                    await streamWithContent.ReadAsync(_buffer.AsBuffer(), (uint)streamWithContent.Size, InputStreamOptions.None);


                    //open up local application folder
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                    //create temporary image in local application folder. This image will get overwritten every time image is picked
                    if (await localFolder.GetFileAsync("tempImage.png") != null)
                    {
                        //delete temp image if exists
                        File.Delete("tempImage.png");
                    }

                    //create new temp image for use within application data local folder
                    StorageFile localImageFile = await localFolder.CreateFileAsync("tempImage.png", CreationCollisionOption.ReplaceExisting);
                    
                    //write out image into file from buffer
                    await FileIO.WriteBytesAsync(localImageFile, _buffer);

                    
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


                try
                {
                    //get image from temporary local file
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    StorageFile localImageFile = await localFolder.GetFileAsync("tempImage.png");

                    //set editable image to path inside local folder
                    var editableImage = await CanvasBitmap.LoadAsync(cvDevice, localImageFile.Path);

                    //draw the image within given rectangle size
                    ds.DrawImage(editableImage, rectangle);
                    //draw ink from inkcanvas
                    ds.DrawInk(icvCanvas.InkPresenter.StrokeContainer.GetStrokes());

                   
                }                
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    throw;
                }
               
            }

            //save merged image into single file
                using (var fStream = await mergedImage.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await cvrTarget.SaveAsync(fStream, CanvasBitmapFileFormat.Png, 1f);
                }                
            }
    

            

        
        //event handler to share image
        private void Share_Click(object sender, RoutedEventArgs e)
        {

            RegisterForSharing();

        }

        //method to handle registration for sharing and share event.
        private void RegisterForSharing()
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();

            dataTransferManager.DataRequested += MainPage_DataRequested;
        }

        //event to handle when share interface opens
        private async void MainPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            //start up request for an image
            DataRequest imageRequest = args.Request;

            //set the title for an image. Title is mandatory!
            imageRequest.Data.Properties.Title = "Doodled image";


            DataRequestDeferral requestDeferral = imageRequest.GetDeferral();


            try
            {
                //this will select the image file for sharing
            }
            finally
            {
                //complete the deferral
                requestDeferral.Complete();
            }
        }
    }
}

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
using Windows.UI.Popups;
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
        IRandomAccessStream _fStream;

        public MainPage()
        {
            this.InitializeComponent();         
            this.Loaded += MainPage_Loaded;
            
            grdImageCanvas.SizeChanged += GrdImageCanvas_SizeChanged;
           
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //here I'm simply setting prefered size of window that application should launch in if on pc.
            ApplicationView.PreferredLaunchViewSize = new Size(600, 650);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
           

            //load the inktoolbar with all the tools
            itbToolBar.Loaded += itbToolBar_Loaded;
            //load the inkCanvas and set the allowed input devices
            icvCanvas.Loaded += IcvCanvas_Loaded;
        }

        //this event will change sizes of ink canvas and image to fit current window size
        private void GrdImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            icvCanvas.Width = Window.Current.Bounds.Width-100;
            icvCanvas.Height = Window.Current.Bounds.Width * 0.8;
            icvCanvas.MaxWidth = 500;
            icvCanvas.MaxHeight = 400;
          
            //not sure which width setting would be more optimal for image          
            imgPicture.Width = Window.Current.Bounds.Width-100;
            imgPicture.Height = Window.Current.Bounds.Width * 0.8;
            imgPicture.MaxWidth = 500;
            imgPicture.MaxHeight = 400;
           
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

            //image stream set to null.
            _fStream = null;

        }

        byte[] _buffer;

        //event handler to open camera and take a picture
        private async void Photo_Click(object sender, RoutedEventArgs e)
        {
            if (imgPicture.Source != null)
            {
                _fStream = null;
            }

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

                Debug.WriteLine("Photo open local folder: --- "+ApplicationData.Current.LocalFolder.Path);

                //create temporary image in local application folder. This image will get overwritten every time image is picked
                if (await localFolder.TryGetItemAsync("tempImage.png") != null)
                {
                    //File.Delete("tempImage.png");
                    await (await localFolder.GetItemAsync("tempImage.png")).DeleteAsync(StorageDeleteOption.PermanentDelete);                    
                }

                StorageFile localImageFile = await localFolder.CreateFileAsync("tempImage.png", CreationCollisionOption.ReplaceExisting);

                //write out image into file from buffer
                await FileIO.WriteBytesAsync(localImageFile, _buffer);
                
            }

            
        }

      

        //event handler to pick picture from gallery
        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            if (imgPicture.Source != null)
            {
                _fStream = null;
            }

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
                    Debug.WriteLine("Open click local folder: --- "+ApplicationData.Current.LocalFolder.Path);

                    //create temporary image in local application folder. This image will get overwritten every time image is picked
                    if (await localFolder.TryGetItemAsync("tempImage.png") != null)
                    {
                        //delete temp image if exists
                       // File.Delete("tempImage.png");
                        await (await localFolder.GetItemAsync("tempImage.png")).DeleteAsync(StorageDeleteOption.PermanentDelete);
                        
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

            await SaveImageAsync();
        }

        private async Task SaveImageAsync()
        {
            //Get the picture gallery folder
            StorageFolder imageStorage = await KnownFolders.SavedPictures.CreateFolderAsync("DoodlePic", CreationCollisionOption.OpenIfExists);
            //create file for new image
            var mergedImage = await imageStorage.CreateFileAsync("pictaDoodle_" + DateTime.Now.ToFileTime() + ".png", CreationCollisionOption.ReplaceExisting);

            //create canvasDevice
            CanvasDevice cvDevice = CanvasDevice.GetSharedDevice();
            //Create canvas render target
            CanvasRenderTarget cvrTarget = new CanvasRenderTarget(cvDevice, (int)icvCanvas.Width, (int)(icvCanvas.Width * 0.8), 96);

            //start the drawing session
            using (var ds = cvrTarget.CreateDrawingSession())
            {
                //set destination rectangle that will determine the size of the editable image in application
                Rect rectangle = new Rect(0, 0, icvCanvas.Width, (icvCanvas.Width * 0.8));

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


                    //close off open async methods to release the resources.
                    ds.Dispose();
                    editableImage.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    throw;
                }

            }

            //save merged image into single file
            using (_fStream = await mergedImage.OpenAsync(FileAccessMode.ReadWrite))
            {
                await cvrTarget.SaveAsync(_fStream, CanvasBitmapFileFormat.Png, 1f);
            }

            
            //show message feedback to the user
            var message = new MessageDialog("Image has been saved successfully");
            message.Title = "Success!";            
            await message.ShowAsync();

            
            await CreateTempImage(mergedImage);

        }

        private static async Task CreateTempImage(StorageFile mergedImage)
        {
            WriteableBitmap wbmp = new WriteableBitmap(30, 30);
            WriteableBitmap shareBmp = new WriteableBitmap(500, 400);
            wbmp.SetSource(await mergedImage.OpenAsync(FileAccessMode.Read));
            shareBmp.SetSource(await mergedImage.OpenAsync(FileAccessMode.Read));

            StorageFolder shareFolder = ApplicationData.Current.LocalFolder;


            var checkFileExists = await shareFolder.GetFileAsync("shareThumb.png");

            if (File.Exists(checkFileExists.Path))
            {
                File.Delete("\\shareThumb.png");
                File.Delete("\\shareImage.png");
            }

            StorageFile shareThumb = await shareFolder.CreateFileAsync("shareThumb.png", CreationCollisionOption.ReplaceExisting);
            StorageFile shareImage = await shareFolder.CreateFileAsync("shareImage.png", CreationCollisionOption.ReplaceExisting);

            using (IRandomAccessStream bmpStream = await shareThumb.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder bmpEnc = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, bmpStream);
                Stream pixelStream = wbmp.PixelBuffer.AsStream();
                byte[] wbmpBytes = new byte[pixelStream.Length];

                await pixelStream.ReadAsync(wbmpBytes, 0, wbmpBytes.Length);

                bmpEnc.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                    (uint)wbmp.PixelWidth, (uint)wbmp.PixelHeight, 96.0, 96.0, wbmpBytes);

                await bmpEnc.FlushAsync();
            }

            using (IRandomAccessStream bmpStream = await shareImage.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder bmpEnc = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, bmpStream);
                Stream pixelStream = shareBmp.PixelBuffer.AsStream();
                byte[] wbmpBytes = new byte[pixelStream.Length];

                await pixelStream.ReadAsync(wbmpBytes, 0, wbmpBytes.Length);

                bmpEnc.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                    (uint)wbmp.PixelWidth, (uint)wbmp.PixelHeight, 96.0, 96.0, wbmpBytes);

                await bmpEnc.FlushAsync();
            }
        }


        //event handler to share image
        private async void Share_Click(object sender, RoutedEventArgs e)
        {
            //if image has not been saved yet then save it
            if (_fStream == null)
            {
                await SaveImageAsync();
                
            }
           
            RegisterForSharing();
            DataTransferManager.ShowShareUI();           
        }

        DataTransferManager dataTransMgr;
        //method to handle registration for sharing and share event.
        private void RegisterForSharing()
        {
            //DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransMgr = DataTransferManager.GetForCurrentView();

            dataTransMgr.DataRequested += MainPage_DataRequested;
        }

        DataRequest imageRequest;
        //event to handle when share interface opens
        private async void MainPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            //DataTransferManager.ShowShareUI();
            //start up request for an image
            imageRequest = args.Request;

            //set the title for an image. Title is mandatory!
            imageRequest.Data.Properties.Title = "Sharing doodled image";

            DataRequestDeferral requestDeferral = imageRequest.GetDeferral();

            try
            {

                //this will select the image file for sharing
                //create thumbnail for sharing
                StorageFile thumbFile = await ApplicationData.Current.LocalFolder.GetFileAsync("shareThumb.png");
                imageRequest.Data.Properties.Thumbnail = RandomAccessStreamReference.CreateFromFile(thumbFile); // RandomAccessStreamReference.CreateFromStream(_fStream);
                //set image from file for sharing
                StorageFile shareImage = await ApplicationData.Current.LocalFolder.GetFileAsync("shareImage.png");
                imageRequest.Data.SetBitmap(RandomAccessStreamReference.CreateFromFile(shareImage));// RandomAccessStreamReference.CreateFromStream(_fStream));

            }
            finally
            {
                //complete the deferral
                requestDeferral.Complete();
            }


        }
    }
}

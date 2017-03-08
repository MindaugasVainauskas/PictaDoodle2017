using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
    }
}

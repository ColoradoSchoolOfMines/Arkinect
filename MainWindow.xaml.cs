//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Kinect.Toolkit.Interaction;
    using System;
    using ACMX.Games.Arkinect;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// A bitmap buffer to put pixel data from the Kinect into for rendering by the WPF Image
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// A byte array to put raw image data from the Kinect into for transformation into a bitmap
        /// </summary>
        private byte[] pixelBytes;

        

        /// <summary>
        /// A brush that can be used to draw things on screen in the specified color
        /// </summary>
        private readonly Brush basicColorBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));

        /// <summary>
        /// A pen that can be used to draw things on screen using the specified brush and width
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Red, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        //The ID of the human that we want to play the game
        private int humanNumber = -1;

        private const int PADDLEHEIGHT = 15;

        private const int PADDLEWIDTH = 600;

        private InteractionStream interactionStream;

        private Ball ball;

        private Block paddle;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Execute startup tasks - Use this for setup that needs to occur after a Kinect is available
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Canvas.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            // Ensure that we have a sensor before continuing
            if (null != this.sensor)
            {
                // Turn on the proper streams to receive event frames
                // Disable lines for event streams you are not using
                this.sensor.SkeletonStream.Enable();
                this.sensor.ColorStream.Enable();
                this.sensor.DepthStream.Enable();
                this.interactionStream = new InteractionStream(this.sensor, new ArkinectInteractionClient());

                // Add an event handlers to be called whenever there is new color frame data
                // Disable event handler registrations for events you are not using
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;
                this.sensor.AllFramesReady += this.SensorAllFramesReady;
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;
                this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
                this.interactionStream.InteractionFrameReady += this.SensorInteractionFrameReady;

                // Prepare the image byte buffer and the bitmap pixel buffer, then bind the bitmap to the WPF Image canvas
                this.pixelBytes = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                //Canvas.Source = colorBitmap;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            double width = this.layoutGrid.RenderSize.Width;
            double height = this.layoutGrid.RenderSize.Height;
            
            ball = new Ball(new Point(width / 2, height / 2), new Point(10, 10));

            paddle = new Block(PADDLEWIDTH, PADDLEHEIGHT, new Point(width / 2, height - PADDLEHEIGHT / 2), false);

            // No sensor, complain
            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Execute shutdown tasks - Use this to release resources as the game shuts down
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            // Acquire data from the SkeletonFrameReadyEventArgs...
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                {
                    return;
                }
                skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
      
                try
                {
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                    var accelerometerReading = sensor.AccelerometerGetCurrentReading();
                    interactionStream.ProcessSkeleton(skeletons, accelerometerReading, skeletonFrame.Timestamp);
                }
                catch (InvalidOperationException)
                {
                    // SkeletonFrame functions may throw when the sensor gets
                    // into a bad state.  Ignore the frame in that case.
                }
            }

            // ...and do stuff with it!
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Do any drawing here with the acquired DrawingContext and skeleton information
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            // Acquire data from ColorImageFrameReadyEventArgs and do stuff with it
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (null != colorFrame)
                {
                    //using (DrawingContext dc = this.drawingGroup.Open())
                    //{
                    //    // Copy the pixel data from the image to a temporary array
                    //    colorFrame.CopyPixelDataTo(this.pixelBytes);

                    //    // Write the pixel data into our bitmap
                    //    //this.colorBitmap.WritePixels(
                    //    //    new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                    //    //    this.pixelBytes,
                    //    //    this.colorBitmap.PixelWidth * sizeof(int),
                    //    //    0);
                    //}
                }
            }
        }

        /// <summary>
        /// Event handler for the Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            // Acquire data from DepthImageFrameReadyEventArgs and do stuff with it
             using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
             {
                if (depthFrame == null)
                    return;
     
                try
                {
                    interactionStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                }
                catch (InvalidOperationException)
                {
                    // DepthFrame functions may throw when the sensor gets
                    // into a bad state.  Ignore the frame in that case.
                }
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's AllFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // Acquire data from AllFramesReadyEventArgs and do stuff with it
        }

        private void SensorInteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            double width = this.layoutGrid.RenderSize.Width;
            double height = this.layoutGrid.RenderSize.Height;

            ball.move();
            ball.collideOutside(paddle.getCollisionBox());
            ball.collideInside(new Rect(0, 0, width, height));

            // Acquire data from SensorInteractionFramesReadyEventArgs and do stuff with it
            UserInfo[] interactionData;

            using (InteractionFrame interactionFrame = e.OpenInteractionFrame()) //dispose as soon as possible
            {
                if (interactionFrame == null)
                    return;
                interactionData = new UserInfo[InteractionFrame.UserInfoArrayLength];
                interactionFrame.CopyInteractionDataTo(interactionData);

                foreach (UserInfo u in interactionData)
                {
                    if (u.SkeletonTrackingId != 0)
                    {
                        if (humanNumber == -1)
                        {
                            humanNumber = u.SkeletonTrackingId;
                        }
                        if (humanNumber != u.SkeletonTrackingId)
                        {
                            continue;
                        }
                        foreach (InteractionHandPointer pointer in u.HandPointers)
                        {
                            if (!pointer.IsPrimaryForUser)
                            {
                                continue;
                            }

                            double x = pointer.X;
                            if (x < 0) x = 0;
                            if (x > 1) x = 1;
                            x = x * (width - PADDLEWIDTH) + PADDLEWIDTH/2;
                            paddle.loc = new Point(x, paddle.loc.Y);
                        }
                    }
                }
            }
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw empty white canvas to fill screen
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
                // Draw ball
                dc.DrawEllipse(basicColorBrush, inferredBonePen, ball.loc, Ball.BALL_RADIUS, Ball.BALL_RADIUS);
                // Draw paddle
                dc.DrawRectangle(Brushes.Red, null, new Rect(paddle.loc.X - PADDLEWIDTH / 2, paddle.loc.Y - PADDLEHEIGHT / 2, PADDLEWIDTH, PADDLEHEIGHT));
            }
        }
    }
}
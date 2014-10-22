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
    using System.Collections.Generic;
    using System.Timers;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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

        double screenWidth;
        double screenHeight;

        //The ID of the human that we want to play the game
        private int humanNumber = -1;

        private const int PADDLEHEIGHT = 15;
        private const int PADDLEWIDTH = 600;

        private int POINTS_PER_BLOCK = 100;

        private InteractionStream interactionStream;

        private Ball ball;
        private Ball newBall(double screenWidth, double screenHeight)
        {
            return new Ball(new Point(screenWidth / 2, screenHeight / 2), new Point((new Random().Next(1, 3) == 1 ? -10 : 10), 10));
        }

        private Block paddle;

        private List<Block> blocks = new List<Block>();

        private GameState gameState;

        private Timer quitTimer = new Timer(5000);

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
                this.sensor.DepthStream.Enable();
                this.interactionStream = new InteractionStream(this.sensor, new ArkinectInteractionClient());

                // Add an event handlers to be called whenever there is new color frame data
                // Disable event handler registrations for events you are not using
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;
                this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
                this.interactionStream.InteractionFrameReady += this.SensorInteractionFrameReady;

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

            screenWidth = this.layoutGrid.RenderSize.Width;
            screenHeight = this.layoutGrid.RenderSize.Height;
            
            ball = newBall(screenWidth, screenHeight);

            paddle = new Block(PADDLEWIDTH, PADDLEHEIGHT, new Point(screenWidth / 2, screenHeight - PADDLEHEIGHT / 2), false);

            resetGame();

            // No sensor, complain
            if (null == this.sensor)
            {
                this.ScoreText.Text = ACMX.Games.Arkinect.Properties.Resources.NoKinectReady;
            }

            // Prepare a delegated callback
            quitTimer.Elapsed += delegate(System.Object o, ElapsedEventArgs eea)
            {
                if (humanNumber == -1)
                {
                    Application.Current.Dispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Normal);
                    if (null != sensor)
                    {
                        sensor.Stop();
                    }
                }
            };
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

        private void SensorInteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            //double width = this.layoutGrid.RenderSize.Width;
            //double height = this.layoutGrid.RenderSize.Height;

            // Do ball movement and collisions if we have a human being tracked
            if (humanNumber != -1)
            {
                ball.move();
                if (ball.loc.Y > screenHeight - Ball.BALL_RADIUS)
                {
                    ball = newBall(screenWidth, screenHeight);
                    gameState.lives = gameState.lives - 1;
                    if (gameState.lives < 0)
                    {
                        resetGame();
                        return;
                    }
                }
                ball.collideOutside(paddle.getCollisionBox());
                List<Block> removals = new List<Block>();
                foreach (Block block in blocks)
                {
                    if (ball.collideOutside(block.getCollisionBox()) && block.isDestroyable)
                    {
                        removals.Add(block);
                        gameState.score = gameState.score + POINTS_PER_BLOCK;
                    }
                }
                blocks.RemoveAll(i => removals.Contains(i));
                ball.collideInside(new Rect(0, 0, screenWidth, screenHeight));
            }

            // Acquire data from SensorInteractionFramesReadyEventArgs and do stuff with it
            UserInfo[] interactionData;

            using (InteractionFrame interactionFrame = e.OpenInteractionFrame()) //dispose as soon as possible
            {
                if (interactionFrame == null)
                    return;
                interactionData = new UserInfo[InteractionFrame.UserInfoArrayLength];
                interactionFrame.CopyInteractionDataTo(interactionData);
                bool sawUserThisFrame = false;
                foreach (UserInfo u in interactionData)
                {
                    if (u.SkeletonTrackingId != 0)
                    {
                        if (humanNumber == -1)
                        {
                            humanNumber = u.SkeletonTrackingId;
                            quitTimer.Stop();
                        }
                        if (humanNumber != u.SkeletonTrackingId)
                        {
                            continue;
                        }
                        sawUserThisFrame = true;
                        foreach (InteractionHandPointer pointer in u.HandPointers)
                        {
                            if (!pointer.IsPrimaryForUser)
                            {
                                continue;
                            }

                            double x = pointer.X;
                            if (x < 0) x = 0;
                            if (x > 1) x = 1;
                            x = x * (screenWidth - PADDLEWIDTH) + PADDLEWIDTH/2;
                            paddle.loc = new Point(x, paddle.loc.Y);
                        }
                    }
                }
                if (!sawUserThisFrame)
                {
                    humanNumber = -1;
                    quitTimer.Start();
                }
            }
            draw();
        }

        /// <summary>
        /// Function to lay out a canvas and draw game objects on it
        /// </summary>
        private void draw()
        {
            // Draw all the game objects
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw empty white canvas to fill screen
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, screenWidth, screenHeight));
                // Draw ball
                dc.DrawEllipse(basicColorBrush, inferredBonePen, ball.loc, Ball.BALL_RADIUS, Ball.BALL_RADIUS);
                // Draw paddle
                dc.DrawRectangle(Brushes.Red, null, new Rect(paddle.loc.X - PADDLEWIDTH / 2, paddle.loc.Y - PADDLEHEIGHT / 2, PADDLEWIDTH, PADDLEHEIGHT));
                // Draw blocks
                foreach (Block block in blocks)
                {
                    dc.DrawRectangle(Brushes.Blue, null, block.getCollisionBox());
                }
            }

            // Display the player's score and lives remaining
            this.ScoreText.Text = gameState.score.ToString()+"pts";
            this.LivesText.Text = new String('O', gameState.lives);
        }

        private void resetGame()
        {
            gameState = new GameState
            {
                score = 0,
                lives = 3
            };

            blocks = new List<Block>();
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    blocks.Add(new Block(screenWidth / 10, screenHeight / 20, new Point(screenWidth / 10 + i * screenWidth / 5, screenHeight / 20 + j * screenHeight / 10), true));
                }
            }
        }
    }
}
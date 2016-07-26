using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace KinectMotionCapture
{
    public partial class MainWindow : Window
    {
        private const int BonesNumber = 20;
        private const int JointsNumber = 20;
        private const int KinectElevationAngle = 0;
        private const int TrackedCoordinate = 1;
        private const int InferredCoordinate = 2;
        private const int HalfInferredCoordinate = 3;
        private const double BoneThickness = 3.0;
        private const double JointDiameter = 10.0;
        private const double ScreenshotDpiX = 300.0;
        private const double ScreenshotDpiY = 300.0;
        private const double StreamDpiX = 96.0;
        private const double StreamDpiY = 96.0;
        private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff";
        private const ColorImageFormat ColorStreamFormat = ColorImageFormat.RgbResolution640x480Fps30;
        private const DepthImageFormat DepthStreamFormat = DepthImageFormat.Resolution640x480Fps30;

        private readonly Brush TrackedJointBrush = Brushes.Green;
        private readonly Brush InferredJointBrush = Brushes.Yellow;
        private readonly Brush TrackedBoneBrush = Brushes.Green;
        private readonly Brush HalfInferredBoneBrush = Brushes.Yellow;
        private readonly Brush InferredBoneBrush = Brushes.Red;
        private readonly JointType[] JointTypeRev =
            {JointType.HipCenter, JointType.Spine, JointType.ShoulderCenter, JointType.Head,
            JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft,
            JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight,
            JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft,
            JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight};

        private KinectSensor _kinect;
        private Skeleton[] _skeletons;

        private JointPositions[] _jointHistory;
        private BonePositions[] _boneHistory;

        private bool _isRunning = false;
        private bool _isRecording = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopKinect();
        }

        private bool RunKinect()
        {
            _kinect = KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected).FirstOrDefault();

            if (_kinect != null)
            {
                TransformSmoothParameters smoothingParam = new TransformSmoothParameters();
                {
                    smoothingParam.Smoothing = Convert.ToSingle(textBox_smoothing.Text);
                    smoothingParam.Correction = Convert.ToSingle(textBox_correction.Text);
                    smoothingParam.Prediction = Convert.ToSingle(textBox_prediction.Text);
                    smoothingParam.JitterRadius = Convert.ToSingle(textBox_jitterRadius.Text);
                    smoothingParam.MaxDeviationRadius = Convert.ToSingle(textBox_maxDeviationRadius.Text);
                };

                _kinect.ColorStream.Enable(ColorStreamFormat);
                _kinect.DepthStream.Enable(DepthStreamFormat);
                _kinect.SkeletonStream.Enable(smoothingParam);

                _kinect.AllFramesReady += Kinect_AllFramesReady;

                _kinect.Start();

                _kinect.ElevationAngle = KinectElevationAngle;

                return true;
            }
            else
            {
                return false;
            }
        }

        private void StopKinect()
        {
            if (_kinect != null)
            {
                _kinect.Stop();
                _kinect = null;
            }

            canvas.Children.Clear();
            camera.Source = null;
        }

        private void Kinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    camera.Source = ColorFrameConverter.CovertToBitmap(frame, StreamDpiX, StreamDpiY);
                }
            }

            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();

                    _skeletons = new Skeleton[frame.SkeletonArrayLength];
                    frame.CopySkeletonDataTo(_skeletons);

                    foreach (var skeleton in _skeletons)
                    {
                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            DrawBonesAndJoints(skeleton);
                        }
                    }

                    _skeletons = null;
                }
            }
        }

        private void DrawBonesAndJoints(Skeleton skeleton)
        {
            string timestamp = DateTime.Now.ToString(TimestampFormat);

            foreach (BoneOrientation bone in skeleton.BoneOrientations)
            {
                DrawBone(skeleton, bone, timestamp);
            }

            foreach (Joint joint in skeleton.Joints)
            {
                DrawJoint(joint, timestamp);
            }
        }

        private void DrawBone(Skeleton skeleton, BoneOrientation bone, string timestamp)
        {
            Joint startJoint = skeleton.Joints[bone.StartJoint];
            Joint endJoint = skeleton.Joints[bone.EndJoint];

            Brush drawBrush = null;
            int coordinateType = 0;

            if (startJoint.TrackingState == JointTrackingState.NotTracked || endJoint.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }
            else if (startJoint.TrackingState == JointTrackingState.Tracked && endJoint.TrackingState == JointTrackingState.Tracked)
            {
                drawBrush = TrackedBoneBrush;
                coordinateType = TrackedCoordinate;
            }
            else if (startJoint.TrackingState == JointTrackingState.Inferred && endJoint.TrackingState == JointTrackingState.Inferred)
            {
                drawBrush = InferredBoneBrush;
                coordinateType = InferredCoordinate;
            }
            else
            {
                drawBrush = HalfInferredBoneBrush;
                coordinateType = HalfInferredCoordinate;
            }

            ColorImagePoint startJointColorPoint = _kinect.CoordinateMapper.MapSkeletonPointToColorPoint(startJoint.Position, ColorStreamFormat);
            ColorImagePoint endJointColorPoint = _kinect.CoordinateMapper.MapSkeletonPointToColorPoint(endJoint.Position, ColorStreamFormat);

            var line = new Line
            {
                Stroke = drawBrush,
                StrokeThickness = BoneThickness,
                X1 = startJointColorPoint.X,
                Y1 = startJointColorPoint.Y,
                X2 = endJointColorPoint.X,
                Y2 = endJointColorPoint.Y
            };

            canvas.Children.Add(line);

            if (_isRecording)
            {
                SaveBoneData(bone, timestamp, coordinateType);
            }
        }

        private void DrawJoint(Joint joint, string timestamp)
        {
            Brush drawBrush = null;
            int coordinateType = 0;

            if (joint.TrackingState == JointTrackingState.Tracked)
            {
                drawBrush = TrackedJointBrush;
                coordinateType = TrackedCoordinate;
            }
            else if (joint.TrackingState == JointTrackingState.Inferred)
            {
                drawBrush = InferredJointBrush;
                coordinateType = InferredCoordinate;
            }
            else
            {
                return;
            }

            Ellipse ellipse = new Ellipse
            {
                Fill = drawBrush,
                Width = JointDiameter,
                Height = JointDiameter
            };

            ColorImagePoint jointColorPoint = _kinect.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorStreamFormat);

            Canvas.SetLeft(ellipse, jointColorPoint.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, jointColorPoint.Y - ellipse.Height / 2);

            canvas.Children.Add(ellipse);

            if (_isRecording)
            {
                SaveJointData(joint, timestamp, coordinateType);
            }
        }

        private void SaveJointData(Joint joint, string timestamp, int coordinateType)
        {
            foreach (var jointPosition in _jointHistory)
            {
                if (joint.JointType == jointPosition.Type)
                {
                    jointPosition.Timestamp.Add(timestamp);
                    jointPosition.Coordinates.Add(joint.Position);
                    jointPosition.CoordinateType.Add(coordinateType);
                    break;
                }
            }
        }

        private void SaveBoneData(BoneOrientation bone, string timestamp, int coordinateType)
        {
            foreach (var bonePosition in _boneHistory)
            {
                if (bone.StartJoint == bonePosition.StartJoint && bone.EndJoint == bonePosition.EndJoint)
                {
                    bonePosition.Timestamp.Add(timestamp);
                    bonePosition.AbsMatrix.Add(bone.AbsoluteRotation.Matrix);
                    bonePosition.AbsQuaternion.Add(bone.AbsoluteRotation.Quaternion);
                    bonePosition.HierMatrix.Add(bone.HierarchicalRotation.Matrix);
                    bonePosition.HierQuaternion.Add(bone.HierarchicalRotation.Quaternion);
                    bonePosition.CoordinateType.Add(coordinateType);
                    break;
                }
            }
        }

        private void button_rec_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRecording)
            {
                _jointHistory = new JointPositions[JointsNumber];
                _boneHistory = new BonePositions[BonesNumber];

                for (int i = 0; i < JointsNumber; i++)
                {
                    _jointHistory[i] = new JointPositions(JointTypeRev[i]);
                }

                _boneHistory[0] = new BonePositions(JointTypeRev[0], JointTypeRev[0]);
                _boneHistory[1] = new BonePositions(JointTypeRev[0], JointTypeRev[1]);
                _boneHistory[2] = new BonePositions(JointTypeRev[1], JointTypeRev[2]);
                _boneHistory[3] = new BonePositions(JointTypeRev[2], JointTypeRev[3]);

                _boneHistory[4] = new BonePositions(JointTypeRev[2], JointTypeRev[4]);
                _boneHistory[5] = new BonePositions(JointTypeRev[4], JointTypeRev[5]);
                _boneHistory[6] = new BonePositions(JointTypeRev[5], JointTypeRev[6]);
                _boneHistory[7] = new BonePositions(JointTypeRev[6], JointTypeRev[7]);

                _boneHistory[8] = new BonePositions(JointTypeRev[2], JointTypeRev[8]);
                _boneHistory[9] = new BonePositions(JointTypeRev[8], JointTypeRev[9]);
                _boneHistory[10] = new BonePositions(JointTypeRev[9], JointTypeRev[10]);
                _boneHistory[11] = new BonePositions(JointTypeRev[10], JointTypeRev[11]);

                _boneHistory[12] = new BonePositions(JointTypeRev[0], JointTypeRev[12]);
                _boneHistory[13] = new BonePositions(JointTypeRev[12], JointTypeRev[13]);
                _boneHistory[14] = new BonePositions(JointTypeRev[13], JointTypeRev[14]);
                _boneHistory[15] = new BonePositions(JointTypeRev[14], JointTypeRev[15]);

                _boneHistory[16] = new BonePositions(JointTypeRev[0], JointTypeRev[16]);
                _boneHistory[17] = new BonePositions(JointTypeRev[16], JointTypeRev[17]);
                _boneHistory[18] = new BonePositions(JointTypeRev[17], JointTypeRev[18]);
                _boneHistory[19] = new BonePositions(JointTypeRev[18], JointTypeRev[19]);

                button_rec.Content = "Stop recording";
                button_rec.Foreground = Brushes.Red;
                button_rec.FontWeight = FontWeights.Bold;
                _isRecording = true;
            }
            else
            {
                Directory.CreateDirectory("data");

                var csv = new StringBuilder();
                string filePath;
                string header;

                foreach (var jointPosition in _jointHistory)
                {
                    if (jointPosition.Timestamp.Count > 0)
                    {
                        csv = new StringBuilder();
                        string firstTimestamp = jointPosition.Timestamp[0].ToString().Replace("-", "").Replace(":", "");
                        filePath = Directory.GetCurrentDirectory() + "\\data\\" + firstTimestamp + "-joint-" + jointPosition.Type.ToString() + ".csv";
                        header = string.Format("{0},{1},{2},{3},{4}{5}", "timestamp", "x", "y", "z", "coord_type", Environment.NewLine);
                        csv.Append(header);

                        for (int j = 0; j < jointPosition.Timestamp.Count; j++)
                        {
                            string timestamp = jointPosition.Timestamp[j];

                            string x = jointPosition.Coordinates[j].X.ToString();
                            string y = jointPosition.Coordinates[j].Y.ToString();
                            string z = jointPosition.Coordinates[j].Z.ToString();

                            string type = jointPosition.CoordinateType[j].ToString();

                            var data = string.Format("{0},{1},{2},{3},{4}{5}", timestamp, x, y, z, type, Environment.NewLine);
                            csv.Append(data);
                        }

                        File.WriteAllText(filePath, csv.ToString());
                        csv = null;
                    }
                }

                foreach (var bonePosition in _boneHistory)
                {
                    if (bonePosition.Timestamp.Count > 0)
                    {
                        csv = new StringBuilder();
                        string firstTimestamp = bonePosition.Timestamp[0].ToString().Replace("-", "").Replace(":", "");
                        filePath = Directory.GetCurrentDirectory() + "\\data\\" + firstTimestamp + "-bone-" + bonePosition.StartJoint.ToString() + "-" + bonePosition.EndJoint.ToString() + ".csv";
                        header =
                            string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35},{36},{37},{38},{39},{40},{41}{42}",
                            "timestamp", "abs_m11", "abs_m12", "abs_m13", "abs_m14", "abs_m21", "abs_m22", "abs_m23", "abs_m24", "abs_m31", "abs_m32", "abs_m33", "abs_m34", "abs_m41", "abs_m42", "abs_m43", "abs_m44", "abs_x", "abs_y", "abs_z", "abs_w", "h_m11", "h_m12", "h_m13", "h_m14", "h_m21", "h_m22", "h_m23", "h_m24", "h_m31", "h_m32", "h_m33", "h_m34", "h_m41", "h_m42", "h_m43", "h_m44", "h_x", "h_y", "h_z", "h_w", "coord_type", Environment.NewLine);
                        csv.Append(header);

                        for (int j = 0; j < bonePosition.Timestamp.Count; j++)
                        {
                            string timestamp = bonePosition.Timestamp[j];

                            string abs_m11 = bonePosition.AbsMatrix[j].M11.ToString();
                            string abs_m12 = bonePosition.AbsMatrix[j].M12.ToString();
                            string abs_m13 = bonePosition.AbsMatrix[j].M13.ToString();
                            string abs_m14 = bonePosition.AbsMatrix[j].M14.ToString();
                            string abs_m21 = bonePosition.AbsMatrix[j].M21.ToString();
                            string abs_m22 = bonePosition.AbsMatrix[j].M22.ToString();
                            string abs_m23 = bonePosition.AbsMatrix[j].M23.ToString();
                            string abs_m24 = bonePosition.AbsMatrix[j].M24.ToString();
                            string abs_m31 = bonePosition.AbsMatrix[j].M31.ToString();
                            string abs_m32 = bonePosition.AbsMatrix[j].M32.ToString();
                            string abs_m33 = bonePosition.AbsMatrix[j].M33.ToString();
                            string abs_m34 = bonePosition.AbsMatrix[j].M34.ToString();
                            string abs_m41 = bonePosition.AbsMatrix[j].M41.ToString();
                            string abs_m42 = bonePosition.AbsMatrix[j].M42.ToString();
                            string abs_m43 = bonePosition.AbsMatrix[j].M43.ToString();
                            string abs_m44 = bonePosition.AbsMatrix[j].M44.ToString();

                            string abs_x = bonePosition.AbsQuaternion[j].X.ToString();
                            string abs_y = bonePosition.AbsQuaternion[j].Y.ToString();
                            string abs_z = bonePosition.AbsQuaternion[j].Z.ToString();
                            string abs_w = bonePosition.AbsQuaternion[j].W.ToString();

                            string h_m11 = bonePosition.HierMatrix[j].M11.ToString();
                            string h_m12 = bonePosition.HierMatrix[j].M12.ToString();
                            string h_m13 = bonePosition.HierMatrix[j].M13.ToString();
                            string h_m14 = bonePosition.HierMatrix[j].M14.ToString();
                            string h_m21 = bonePosition.HierMatrix[j].M21.ToString();
                            string h_m22 = bonePosition.HierMatrix[j].M22.ToString();
                            string h_m23 = bonePosition.HierMatrix[j].M23.ToString();
                            string h_m24 = bonePosition.HierMatrix[j].M24.ToString();
                            string h_m31 = bonePosition.HierMatrix[j].M31.ToString();
                            string h_m32 = bonePosition.HierMatrix[j].M32.ToString();
                            string h_m33 = bonePosition.HierMatrix[j].M33.ToString();
                            string h_m34 = bonePosition.HierMatrix[j].M34.ToString();
                            string h_m41 = bonePosition.HierMatrix[j].M41.ToString();
                            string h_m42 = bonePosition.HierMatrix[j].M42.ToString();
                            string h_m43 = bonePosition.HierMatrix[j].M43.ToString();
                            string h_m44 = bonePosition.HierMatrix[j].M44.ToString();

                            string h_x = bonePosition.HierQuaternion[j].X.ToString();
                            string h_y = bonePosition.HierQuaternion[j].Y.ToString();
                            string h_z = bonePosition.HierQuaternion[j].Z.ToString();
                            string h_w = bonePosition.HierQuaternion[j].W.ToString();

                            string type = bonePosition.CoordinateType[j].ToString();

                            var data =
                                string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35},{36},{37},{38},{39},{40},{41}{42}",
                                timestamp, abs_m11, abs_m12, abs_m13, abs_m14, abs_m21, abs_m22, abs_m23, abs_m24, abs_m31, abs_m32, abs_m33, abs_m34, abs_m41, abs_m42, abs_m43, abs_m44, abs_x, abs_y, abs_z, abs_w, h_m11, h_m12, h_m13, h_m14, h_m21, h_m22, h_m23, h_m24, h_m31, h_m32, h_m33, h_m34, h_m41, h_m42, h_m43, h_m44, h_x, h_y, h_z, h_w, type, Environment.NewLine);
                            csv.Append(data);
                        }

                        File.WriteAllText(filePath, csv.ToString());
                        csv = null;
                    }
                }

                _jointHistory = null;
                _boneHistory = null;

                button_rec.Content = "Start recording";
                button_rec.Foreground = Brushes.Black;
                button_rec.FontWeight = FontWeights.Normal;
                _isRecording = false;
            }
        }

        private void button_screenshot_Click(object sender, RoutedEventArgs e)
        {
            string timestamp = DateTime.Now.ToString(TimestampFormat).Replace("-", "").Replace(":", "");

            Rect rect = new Rect(camera.Margin.Left, camera.Margin.Top, camera.ActualWidth, camera.ActualHeight);
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)(rect.Right / StreamDpiX * ScreenshotDpiX),
                (int)(rect.Bottom / StreamDpiY * ScreenshotDpiY),
                ScreenshotDpiX,
                ScreenshotDpiY,
                PixelFormats.Default);
            rtb.Render(camera);
            rtb.Render(canvas);

            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

            MemoryStream ms = new MemoryStream();

            pngEncoder.Save(ms);
            ms.Close();
            Directory.CreateDirectory("data");
            string filePath = Directory.GetCurrentDirectory() + "\\data\\" + timestamp + ".png";
            File.WriteAllBytes(filePath, ms.ToArray());
        }

        private void button_setBody_Click(object sender, RoutedEventArgs e)
        {
            if (_kinect != null && _kinect.SkeletonStream != null && _skeletons != null)
            {
                if (!_kinect.SkeletonStream.AppChoosesSkeletons)
                {
                    _kinect.SkeletonStream.AppChoosesSkeletons = true;
                }

                int id = 0;
                float z = 10;

                foreach (Skeleton skeleton in _skeletons.Where(s => s.TrackingState != SkeletonTrackingState.NotTracked))
                {
                    if (skeleton.Position.Z < z)
                    {
                        z = skeleton.Position.Z;
                        id = skeleton.TrackingId;
                    }
                }

                if (id != 0)
                {
                    _kinect.SkeletonStream.ChooseSkeletons(id);
                }
            }
        }

        private void button_kinect_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                bool isRunning = RunKinect();

                if (isRunning)
                {
                    button_kinect.Content = "Stop motion capture";
                    button_kinect.Foreground = Brushes.Red;
                    button_kinect.FontWeight = FontWeights.Bold;
                    _isRunning = true;
                    button_setBody.IsEnabled = true;
                    button_rec.IsEnabled = true;
                    button_screenshot.IsEnabled = true;
                    groupBox_smoothParams.IsEnabled = false;
                }
                else
                {
                    MessageBox.Show("Could not connect to Kinect camera.", "Connection error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                StopKinect();

                button_kinect.Content = "Start motion capture";
                button_kinect.Foreground = Brushes.Black;
                button_kinect.FontWeight = FontWeights.Normal;
                _isRunning = false;
                button_setBody.IsEnabled = false;
                button_rec.IsEnabled = false;
                button_screenshot.IsEnabled = false;
                groupBox_smoothParams.IsEnabled = true;
            }
        }
    }
}
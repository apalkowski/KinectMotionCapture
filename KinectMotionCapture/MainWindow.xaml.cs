using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        private const int JointsNumber = 20;
        private const double BoneThickness = 3.0;
        private const double JointDiameter = 10.0;

        private const int KinectElevationAngle = 0;

        private readonly Brush trackedJointBrush = Brushes.Green;
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6.0);
        private readonly Pen inferredBonePen = new Pen(Brushes.Yellow, 1.0);

        private KinectSensor _kinect;
        private Skeleton[] _skeletons = new Skeleton[6];

        private bool _isRecording = false;

        private JointType[] _jointTypeRev =
            {JointType.HipCenter, JointType.Spine, JointType.ShoulderCenter, JointType.Head,
            JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft,
            JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight,
            JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft,
            JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight};

        private JointPositions[] _jointHistory;
        private BonePositions[] _boneHistory;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _kinect = KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected).FirstOrDefault();

            if (_kinect != null)
            {
                TransformSmoothParameters smoothingParam = new TransformSmoothParameters();
                {
                    smoothingParam.Smoothing = 0.2f;
                    smoothingParam.Correction = 0.1f;
                    smoothingParam.Prediction = 0.5f;
                    smoothingParam.JitterRadius = 0.06f;
                    smoothingParam.MaxDeviationRadius = 0.06f;
                };

                _kinect.ColorStream.Enable();
                _kinect.DepthStream.Enable();
                _kinect.SkeletonStream.Enable(smoothingParam);

                _kinect.AllFramesReady += Sensor_AllFramesReady;

                _kinect.Start();

                _kinect.ElevationAngle = KinectElevationAngle;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_kinect != null)
            {
                _kinect.Stop();
            }
        }

        private void Sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    camera.Source = ColorFrameConverter.CovertToBitmap(frame);
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

                    // https://msdn.microsoft.com/en-us/library/jj663790.aspx
                    Vector4 acc = _kinect.AccelerometerGetCurrentReading();
                    var floorClipPlane = frame.FloorClipPlane;
                    textBox.Text = "acc_x: " + acc.X.ToString() + "\n" + "acc_y: " + acc.Y.ToString() + "\n" + "acc_z: " + acc.Z.ToString() + "\n" + "acc_w: " + acc.W.ToString() + "\n" + "floor_x: " + floorClipPlane.Item1.ToString() + "\n" + "floor_y: " + floorClipPlane.Item2.ToString() + "\n" + "floor_z: " + floorClipPlane.Item3.ToString() + "\n" + "floor_w: " + floorClipPlane.Item4.ToString() + "\n";
                }
            }
        }

        private void DrawBonesAndJoints(Skeleton skeleton)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");

            DrawBone(skeleton, JointType.HipCenter, JointType.HipCenter, timestamp);
            
            DrawBone(skeleton, JointType.Head, JointType.ShoulderCenter, timestamp);
            DrawBone(skeleton, JointType.ShoulderCenter, JointType.ShoulderLeft, timestamp);
            DrawBone(skeleton, JointType.ShoulderCenter, JointType.ShoulderRight, timestamp);
            DrawBone(skeleton, JointType.ShoulderCenter, JointType.Spine, timestamp);
            DrawBone(skeleton, JointType.Spine, JointType.HipCenter, timestamp);
            DrawBone(skeleton, JointType.HipCenter, JointType.HipLeft, timestamp);
            DrawBone(skeleton, JointType.HipCenter, JointType.HipRight, timestamp);
            
            DrawBone(skeleton, JointType.ShoulderLeft, JointType.ElbowLeft, timestamp);
            DrawBone(skeleton, JointType.ElbowLeft, JointType.WristLeft, timestamp);
            DrawBone(skeleton, JointType.WristLeft, JointType.HandLeft, timestamp);
            
            DrawBone(skeleton, JointType.ShoulderRight, JointType.ElbowRight, timestamp);
            DrawBone(skeleton, JointType.ElbowRight, JointType.WristRight, timestamp);
            DrawBone(skeleton, JointType.WristRight, JointType.HandRight, timestamp);
            
            DrawBone(skeleton, JointType.HipLeft, JointType.KneeLeft, timestamp);
            DrawBone(skeleton, JointType.KneeLeft, JointType.AnkleLeft, timestamp);
            DrawBone(skeleton, JointType.AnkleLeft, JointType.FootLeft, timestamp);
            
            DrawBone(skeleton, JointType.HipRight, JointType.KneeRight, timestamp);
            DrawBone(skeleton, JointType.KneeRight, JointType.AnkleRight, timestamp);
            DrawBone(skeleton, JointType.AnkleRight, JointType.FootRight, timestamp);

            foreach (Joint joint in skeleton.Joints)
            {
                SkeletonPoint skeletonPoint = joint.Position;
                ColorImagePoint colorPoint = _kinect.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoint, ColorImageFormat.RgbResolution640x480Fps30);
                Point point = new Point(colorPoint.X, colorPoint.Y);

                Brush drawBrush = null;
                int coordinateType = 0;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = trackedJointBrush;
                    coordinateType = 1;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = inferredJointBrush;
                    coordinateType = 2;
                }

                if (drawBrush != null)
                {
                    Ellipse ellipse = new Ellipse
                    {
                        Fill = drawBrush,
                        Width = JointDiameter,
                        Height = JointDiameter
                    };

                    Canvas.SetLeft(ellipse, colorPoint.X - ellipse.Width / 2);
                    Canvas.SetTop(ellipse, colorPoint.Y - ellipse.Height / 2);

                    canvas.Children.Add(ellipse);

                    if (_isRecording)
                    {
                        for (int i = 0; i < JointsNumber; i++)
                        {
                            if (joint.JointType == _jointHistory[i].type)
                            {
                                _jointHistory[i].coordinates.Add(joint.Position);
                                _jointHistory[i].coordinateType.Add(coordinateType);
                                _jointHistory[i].timestamp.Add(timestamp);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void DrawBone(Skeleton skeleton, JointType jointType1, JointType jointType2, string timestamp)
        {
            Joint joint1 = skeleton.Joints[jointType1];
            Joint joint2 = skeleton.Joints[jointType2];

            SkeletonPoint skeletonPoint1 = joint1.Position;
            ColorImagePoint colorPoint1 = _kinect.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoint1, ColorImageFormat.RgbResolution640x480Fps30);
            SkeletonPoint skeletonPoint2 = joint2.Position;
            ColorImagePoint colorPoint2 = _kinect.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoint2, ColorImageFormat.RgbResolution640x480Fps30);

            if (joint1.TrackingState == JointTrackingState.NotTracked ||
                joint2.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            if (joint1.TrackingState == JointTrackingState.Inferred &&
                joint2.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            Brush drawBrush = inferredJointBrush;
            int coordinateType = 2;

            if (joint1.TrackingState == JointTrackingState.Tracked && joint2.TrackingState == JointTrackingState.Tracked)
            {
                drawBrush = trackedJointBrush;
                coordinateType = 1;
            }

            Line line = new Line
            {
                Stroke = drawBrush,
                StrokeThickness = BoneThickness,
                X1 = colorPoint1.X,
                Y1 = colorPoint1.Y,
                X2 = colorPoint2.X,
                Y2 = colorPoint2.Y
            };

            canvas.Children.Add(line);

            if (_isRecording)
            {
                for (int i = 0; i < JointsNumber; i++)
                {
                    if ((joint1.JointType == _boneHistory[i].startJoint || joint1.JointType == _boneHistory[i].endJoint) && (joint2.JointType == _boneHistory[i].startJoint || joint2.JointType == _boneHistory[i].endJoint))
                    {
                        _boneHistory[i].timestamp.Add(timestamp);
                        _boneHistory[i].absMatrix.Add(skeleton.BoneOrientations[_jointTypeRev[i]].AbsoluteRotation.Matrix);
                        _boneHistory[i].absQuaternion.Add(skeleton.BoneOrientations[_jointTypeRev[i]].AbsoluteRotation.Quaternion);
                        _boneHistory[i].hierMatrix.Add(skeleton.BoneOrientations[_jointTypeRev[i]].HierarchicalRotation.Matrix);
                        _boneHistory[i].hierQuaternion.Add(skeleton.BoneOrientations[_jointTypeRev[i]].HierarchicalRotation.Quaternion);
                        _boneHistory[i].coordinateType.Add(coordinateType);
                        break;
                    }
                }
            }
        }

        private static class ColorFrameConverter
        {
            private static WriteableBitmap _bitmap = null;
            private static int _width;
            private static int _height;
            private static byte[] _pixels = null;

            public static BitmapSource CovertToBitmap(ColorImageFrame frame)
            {
                if (_bitmap == null)
                {
                    _width = frame.Width;
                    _height = frame.Height;
                    _pixels = new byte[_width * _height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];
                    _bitmap = new WriteableBitmap(_width, _height, 96.0, 96.0, PixelFormats.Bgr32, null);
                }

                frame.CopyPixelDataTo(_pixels);

                _bitmap.Lock();

                Marshal.Copy(_pixels, 0, _bitmap.BackBuffer, _pixels.Length);
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));

                _bitmap.Unlock();

                return _bitmap;
            }
        }

        private void button_rec_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRecording)
            {
                _jointHistory = new JointPositions[JointsNumber];
                _boneHistory = new BonePositions[JointsNumber];
                for (int i = 0; i < JointsNumber; i++)
                {
                    _jointHistory[i] = new JointPositions(_jointTypeRev[i]);
                }
                _boneHistory[0] = new BonePositions(_jointTypeRev[0], _jointTypeRev[0]);
                _boneHistory[1] = new BonePositions(_jointTypeRev[0], _jointTypeRev[1]);
                _boneHistory[2] = new BonePositions(_jointTypeRev[1], _jointTypeRev[2]);
                _boneHistory[3] = new BonePositions(_jointTypeRev[2], _jointTypeRev[3]);

                _boneHistory[4] = new BonePositions(_jointTypeRev[2], _jointTypeRev[4]);
                _boneHistory[5] = new BonePositions(_jointTypeRev[4], _jointTypeRev[5]);
                _boneHistory[6] = new BonePositions(_jointTypeRev[5], _jointTypeRev[6]);
                _boneHistory[7] = new BonePositions(_jointTypeRev[6], _jointTypeRev[7]);

                _boneHistory[8] = new BonePositions(_jointTypeRev[2], _jointTypeRev[8]);
                _boneHistory[9] = new BonePositions(_jointTypeRev[8], _jointTypeRev[9]);
                _boneHistory[10] = new BonePositions(_jointTypeRev[9], _jointTypeRev[10]);
                _boneHistory[11] = new BonePositions(_jointTypeRev[10], _jointTypeRev[11]);

                _boneHistory[12] = new BonePositions(_jointTypeRev[0], _jointTypeRev[12]);
                _boneHistory[13] = new BonePositions(_jointTypeRev[12], _jointTypeRev[13]);
                _boneHistory[14] = new BonePositions(_jointTypeRev[13], _jointTypeRev[14]);
                _boneHistory[15] = new BonePositions(_jointTypeRev[14], _jointTypeRev[15]);

                _boneHistory[16] = new BonePositions(_jointTypeRev[0], _jointTypeRev[16]);
                _boneHistory[17] = new BonePositions(_jointTypeRev[16], _jointTypeRev[17]);
                _boneHistory[18] = new BonePositions(_jointTypeRev[17], _jointTypeRev[18]);
                _boneHistory[19] = new BonePositions(_jointTypeRev[18], _jointTypeRev[19]);
                
                button_rec.Content = "Stop recording";
                button_rec.Foreground = Brushes.Red;
                button_rec.FontWeight = FontWeights.Bold;
                _isRecording = true;
            }
            else
            {
                Directory.CreateDirectory("data");

                for (int i = 0; i < JointsNumber; i++)
                {
                    var csv = new StringBuilder();
                    string filePath = Directory.GetCurrentDirectory() + "\\data\\joint-" + _jointHistory[i].type.ToString() + ".dat";
                    var header = string.Format("{0};{1};{2};{3};{4}{5}", "timestamp", "x", "y", "z", "coord_type", Environment.NewLine);
                    csv.Append(header);

                    for (int j = 0; j < _jointHistory[i].timestamp.Count; j++)
                    {
                        string time = _jointHistory[i].timestamp[j];
                        string x = _jointHistory[i].coordinates[j].X.ToString();
                        string y = _jointHistory[i].coordinates[j].Y.ToString();
                        string z = _jointHistory[i].coordinates[j].Z.ToString();
                        string type = _jointHistory[i].coordinateType[j].ToString();
                        var data = string.Format("{0};{1};{2};{3};{4}{5}", time, x, y, z, type, Environment.NewLine);
                        csv.Append(data);
                    }

                    File.WriteAllText(filePath, csv.ToString());

                    var csvBones = new StringBuilder();
                    string filePathBones = Directory.GetCurrentDirectory() + "\\data\\bone-" + _boneHistory[i].startJoint.ToString() + "-" + _boneHistory[i].endJoint.ToString() + ".dat";
                    var headerBones =
                        string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};{21};{22};{23};{24};{25};{26};{27};{28};{29};{30};{31};{32};{33};{34};{35};{36};{37};{38};{39};{40};{41}{42}",
                        "timestamp", "abs_m11", "abs_m12", "abs_m13", "abs_m14", "abs_m21", "abs_m22", "abs_m23", "abs_m24", "abs_m31", "abs_m32", "abs_m33", "abs_m34", "abs_m41", "abs_m42", "abs_m43", "abs_m44", "abs_x", "abs_y", "abs_z", "abs_w", "h_m11", "h_m12", "h_m13", "h_m14", "h_m21", "h_m22", "h_m23", "h_m24", "h_m31", "h_m32", "h_m33", "h_m34", "h_m41", "h_m42", "h_m43", "h_m44", "h_x", "h_y", "h_z", "h_w", "coord_type", Environment.NewLine);
                    csvBones.Append(headerBones);

                    for (int j = 0; j < _boneHistory[i].timestamp.Count; j++)
                    {
                        string time = _boneHistory[i].timestamp[j];

                        string abs_m11 = _boneHistory[i].absMatrix[j].M11.ToString();
                        string abs_m12 = _boneHistory[i].absMatrix[j].M12.ToString();
                        string abs_m13 = _boneHistory[i].absMatrix[j].M13.ToString();
                        string abs_m14 = _boneHistory[i].absMatrix[j].M14.ToString();
                        string abs_m21 = _boneHistory[i].absMatrix[j].M21.ToString();
                        string abs_m22 = _boneHistory[i].absMatrix[j].M22.ToString();
                        string abs_m23 = _boneHistory[i].absMatrix[j].M23.ToString();
                        string abs_m24 = _boneHistory[i].absMatrix[j].M24.ToString();
                        string abs_m31 = _boneHistory[i].absMatrix[j].M31.ToString();
                        string abs_m32 = _boneHistory[i].absMatrix[j].M32.ToString();
                        string abs_m33 = _boneHistory[i].absMatrix[j].M33.ToString();
                        string abs_m34 = _boneHistory[i].absMatrix[j].M34.ToString();
                        string abs_m41 = _boneHistory[i].absMatrix[j].M41.ToString();
                        string abs_m42 = _boneHistory[i].absMatrix[j].M42.ToString();
                        string abs_m43 = _boneHistory[i].absMatrix[j].M43.ToString();
                        string abs_m44 = _boneHistory[i].absMatrix[j].M44.ToString();

                        string abs_x = _boneHistory[i].absQuaternion[j].X.ToString();
                        string abs_y = _boneHistory[i].absQuaternion[j].Y.ToString();
                        string abs_z = _boneHistory[i].absQuaternion[j].Z.ToString();
                        string abs_w = _boneHistory[i].absQuaternion[j].W.ToString();

                        string h_m11 = _boneHistory[i].hierMatrix[j].M11.ToString();
                        string h_m12 = _boneHistory[i].hierMatrix[j].M12.ToString();
                        string h_m13 = _boneHistory[i].hierMatrix[j].M13.ToString();
                        string h_m14 = _boneHistory[i].hierMatrix[j].M14.ToString();
                        string h_m21 = _boneHistory[i].hierMatrix[j].M21.ToString();
                        string h_m22 = _boneHistory[i].hierMatrix[j].M22.ToString();
                        string h_m23 = _boneHistory[i].hierMatrix[j].M23.ToString();
                        string h_m24 = _boneHistory[i].hierMatrix[j].M24.ToString();
                        string h_m31 = _boneHistory[i].hierMatrix[j].M31.ToString();
                        string h_m32 = _boneHistory[i].hierMatrix[j].M32.ToString();
                        string h_m33 = _boneHistory[i].hierMatrix[j].M33.ToString();
                        string h_m34 = _boneHistory[i].hierMatrix[j].M34.ToString();
                        string h_m41 = _boneHistory[i].hierMatrix[j].M41.ToString();
                        string h_m42 = _boneHistory[i].hierMatrix[j].M42.ToString();
                        string h_m43 = _boneHistory[i].hierMatrix[j].M43.ToString();
                        string h_m44 = _boneHistory[i].hierMatrix[j].M44.ToString();

                        string h_x = _boneHistory[i].hierQuaternion[j].X.ToString();
                        string h_y = _boneHistory[i].hierQuaternion[j].Y.ToString();
                        string h_z = _boneHistory[i].hierQuaternion[j].Z.ToString();
                        string h_w = _boneHistory[i].hierQuaternion[j].W.ToString();

                        string type = _boneHistory[i].coordinateType[j].ToString();

                        var dataBones =
                            string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};{21};{22};{23};{24};{25};{26};{27};{28};{29};{30};{31};{32};{33};{34};{35};{36};{37};{38};{39};{40};{41}{42}",
                            time, abs_m11, abs_m12, abs_m13, abs_m14, abs_m21, abs_m22, abs_m23, abs_m24, abs_m31, abs_m32, abs_m33, abs_m34, abs_m41, abs_m42, abs_m43, abs_m44, abs_x, abs_y, abs_z, abs_w, h_m11, h_m12, h_m13, h_m14, h_m21, h_m22, h_m23, h_m24, h_m31, h_m32, h_m33, h_m34, h_m41, h_m42, h_m43, h_m44, h_x, h_y, h_z, h_w, type, Environment.NewLine);
                        csvBones.Append(dataBones);
                    }

                    File.WriteAllText(filePathBones, csvBones.ToString());
                }

                _jointHistory = null;
                _boneHistory = null;

                this.button_rec.Content = "Start recording";
                button_rec.Foreground = Brushes.Black;
                button_rec.FontWeight = FontWeights.Normal;
                _isRecording = false;
            }
        }

        private void button_screenshot_Click(object sender, RoutedEventArgs e)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffff");

            Rect rect = new Rect(camera.Margin.Left, camera.Margin.Top, camera.ActualWidth, camera.ActualHeight);
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)rect.Right, (int)rect.Bottom, 96d, 96d, PixelFormats.Default);
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
            if (_kinect != null && _kinect.SkeletonStream != null)
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
    }
}
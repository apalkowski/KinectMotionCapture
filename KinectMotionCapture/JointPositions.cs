using System.Collections.Generic;
using Microsoft.Kinect;

namespace KinectMotionCapture
{
    internal class JointPositions
    {
        public JointType Type;
        public List<string> Timestamp;
        public List<SkeletonPoint> Coordinates;
        public List<int> CoordinateType;

        public JointPositions(JointType type)
        {
            Type = type;
            Timestamp = new List<string>();
            Coordinates = new List<SkeletonPoint>();
            CoordinateType = new List<int>();
        }
    }
}
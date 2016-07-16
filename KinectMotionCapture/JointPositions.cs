using System.Collections.Generic;
using Microsoft.Kinect;

namespace KinectMotionCapture
{
    public class JointPositions
    {
        public List<string> timestamp;
        public JointType type;
        public List<SkeletonPoint> coordinates;
        public List<int> coordinateType;

        public JointPositions(JointType type)
        {
            this.type = type;
            this.timestamp = new List<string>();
            this.coordinates = new List<SkeletonPoint>();
            this.coordinateType = new List<int>();
        }
    }
}
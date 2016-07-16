using System.Collections.Generic;
using Microsoft.Kinect;

namespace KinectMotionCapture
{
    internal class BonePositions
    {
        public List<string> timestamp;
        public JointType startJoint;
        public JointType endJoint;
        public List<Matrix4> absMatrix;
        public List<Vector4> absQuaternion;
        public List<Matrix4> hierMatrix;
        public List<Vector4> hierQuaternion;
        public List<int> coordinateType;

        public BonePositions(JointType startJoint, JointType endJoint)
        {
            this.startJoint = startJoint;
            this.endJoint = endJoint;
            this.timestamp = new List<string>();
            this.absMatrix = new List<Matrix4>();
            this.absQuaternion = new List<Vector4>();
            this.hierMatrix = new List<Matrix4>();
            this.hierQuaternion = new List<Vector4>();
            this.coordinateType = new List<int>();
        }
    }
}
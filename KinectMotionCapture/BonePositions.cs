using System.Collections.Generic;
using Microsoft.Kinect;

namespace KinectMotionCapture
{
    internal class BonePositions
    {
        public JointType StartJoint;
        public JointType EndJoint;
        public List<string> Timestamp;
        public List<Matrix4> AbsMatrix;
        public List<Vector4> AbsQuaternion;
        public List<Matrix4> HierMatrix;
        public List<Vector4> HierQuaternion;
        public List<int> CoordinateType;

        public BonePositions(JointType startJoint, JointType endJoint)
        {
            StartJoint = startJoint;
            EndJoint = endJoint;
            Timestamp = new List<string>();
            AbsMatrix = new List<Matrix4>();
            AbsQuaternion = new List<Vector4>();
            HierMatrix = new List<Matrix4>();
            HierQuaternion = new List<Vector4>();
            CoordinateType = new List<int>();
        }
    }
}
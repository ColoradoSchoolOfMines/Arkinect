using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect.Toolkit.Interaction;

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    class ArkinectInteractionClient: Microsoft.Kinect.Toolkit.Interaction.IInteractionClient
    {
        public Microsoft.Kinect.Toolkit.Interaction.InteractionInfo GetInteractionInfoAtLocation(int skeletonTrackingId, Microsoft.Kinect.Toolkit.Interaction.InteractionHandType handType, double x, double y)
        {
            var result = new InteractionInfo();
            result.IsGripTarget = true;
            result.IsPressTarget = true;
            result.PressAttractionPointX = 0.5;
            result.PressAttractionPointY = 0.5;
            result.PressTargetControlId = 1;
            
            return result;
        }
    }
}
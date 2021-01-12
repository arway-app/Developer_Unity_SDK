/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using System;
using UnityEngine;

namespace Arway
{
    [Serializable]
    public class Utils
    {
        /// <summary>
        /// Gets the pose.
        /// </summary>
        /// <returns>The pose.</returns>
        /// <param name="position">Position.</param>
        public Vector3 getPose(Position position)
        {
            return new Vector3((float)position.posX * -1f, (float)position.posY, (float)position.posZ);
        }

        /// <summary>
        /// Gets the rot.
        /// </summary>
        /// <returns>The rot.</returns>
        /// <param name="rotation">Rotation.</param>
        public Vector3 getRot(Rotation rotation)
        {
            return new Vector3((float)rotation.rotX * (float)(180 / Math.PI), (float)rotation.rotY * (float)(-180 / Math.PI), (float)rotation.rotZ * (float)(-180 / Math.PI));
        }

        /// <summary>
        /// Gets the scale.
        /// </summary>
        /// <returns>The scale.</returns>
        /// <param name="scale">Scale.</param>
        public Vector3 getScale(Scale scale)
        {
            return new Vector3((float)scale.scaX, (float)scale.scaY, (float)scale.scaZ);
        }

    }
}
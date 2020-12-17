/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using UnityEngine;
using UnityEngine.UI;

namespace Arway
{
    public class PoseSetter : MonoBehaviour
    {
        [SerializeField]
        public GameObject ARSpace;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="respose"></param>
        /// <param name="cloudid"></param>
        public void poseHandlerMultiMap(LocalizationResponse respose)
        {
            Matrix4x4 cloudpose = new Matrix4x4();
            cloudpose = Matrix4x4.identity;

            int cloudid = respose.cloudid;

            cloudpose.m00 = respose.pose[0]; cloudpose.m01 = respose.pose[1]; cloudpose.m02 = respose.pose[2]; cloudpose.m03 = respose.pose[3];
            cloudpose.m10 = respose.pose[4]; cloudpose.m11 = respose.pose[5]; cloudpose.m12 = respose.pose[6]; cloudpose.m13 = respose.pose[7];
            cloudpose.m20 = respose.pose[8]; cloudpose.m21 = respose.pose[9]; cloudpose.m22 = respose.pose[10]; cloudpose.m23 = respose.pose[11];

            Vector3 pos = new Vector3(cloudpose[0, 3] * -1f, cloudpose[1, 3], cloudpose[2, 3]);

            Quaternion rot = Quaternion.Euler(cloudpose.rotation.eulerAngles.x, (360 - cloudpose.rotation.eulerAngles.y) % 360, cloudpose.rotation.eulerAngles.z);
            Matrix4x4 cloud = Matrix4x4.TRS(pos, rot, Vector3.one);


            if (MultiMapAssetImporter.mapIdToOffset.ContainsKey(cloudid))
            {
                Vector3 Mapoffsetposition = MultiMapAssetImporter.mapIdToOffset[cloudid].position;
                Quaternion Mapoffsetrotation = MultiMapAssetImporter.mapIdToOffset[cloudid].rotation;
                Matrix4x4 cloudMapOffset = Matrix4x4.TRS(Mapoffsetposition, Mapoffsetrotation, Vector3.one);

                GetGlobalPose(cloud, cloudMapOffset);
            }



        }
        // pose setter for single map
        public void poseHandler(LocalizationResponse respose)
        {
            Matrix4x4 cloudpose = new Matrix4x4();
            cloudpose = Matrix4x4.identity;

            cloudpose.m00 = respose.pose[0]; cloudpose.m01 = respose.pose[1]; cloudpose.m02 = respose.pose[2]; cloudpose.m03 = respose.pose[3];
            cloudpose.m10 = respose.pose[4]; cloudpose.m11 = respose.pose[5]; cloudpose.m12 = respose.pose[6]; cloudpose.m13 = respose.pose[7];
            cloudpose.m20 = respose.pose[8]; cloudpose.m21 = respose.pose[9]; cloudpose.m22 = respose.pose[10]; cloudpose.m23 = respose.pose[11];

            Vector3 pos = new Vector3(cloudpose[0, 3] * -1f, cloudpose[1, 3], cloudpose[2, 3]);

            Quaternion rot = Quaternion.Euler(cloudpose.rotation.eulerAngles.x, (360 - cloudpose.rotation.eulerAngles.y) % 360, cloudpose.rotation.eulerAngles.z);
            
            Matrix4x4 cloud = Matrix4x4.TRS(pos, rot, Vector3.one);
            Matrix4x4 cameratracker = Matrix4x4.TRS(Camera.main.transform.position, Camera.main.transform.rotation, Vector3.one);

            
            Matrix4x4 result = cameratracker * cloud.inverse;

            ARSpace.transform.rotation = result.rotation;
            ARSpace.transform.position = new Vector3(result[0, 3], result[1, 3], result[2, 3]);

        }



        public void GetGlobalPose(Matrix4x4 Cloudlocal_pose, Matrix4x4 cloudMap_offset)
        {

            Matrix4x4 cameratracker = Matrix4x4.TRS(Camera.main.transform.position, Camera.main.transform.rotation, Vector3.one);
            Matrix4x4 resultpose = cloudMap_offset * Cloudlocal_pose;
            Matrix4x4 globalPose = (cameratracker) * (resultpose.inverse);

            ARSpace.transform.rotation = globalPose.rotation;
            ARSpace.transform.position = new Vector3(globalPose[0, 3], globalPose[1, 3], globalPose[2, 3]);
            
        }

    }
}

        
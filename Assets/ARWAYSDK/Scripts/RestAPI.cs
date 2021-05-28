/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Arway
{

    public static class EndPoint
    {
        public const string MAP_LIST = "map-api/maps_list/";
        public const string MAP_DATA = "map-api/map_data/";
        public const string CLOUD_LIST = "map-api/cloud_list/";

        public const string SET_PARAM = "set_params";
        public const string REQ_POSE = "req_pose";
        public const string CLOSE_LOC = "close_loc";

        public const string AUTH = "developer/authentication/";

        public const string DELETE_CLOUD_MAP = "map-api/map_delete/";

        public const string MAP_UPLOAD = "map-api/map_upload/";
    }


    [Serializable]
    public class LocalizationRequest
    {

        public List<int> cloud_Ids;
        public int width;
        public int height;
        public int channel;
        public float Camera_fx;
        public float Camera_fy;
        public float Camera_px;
        public float Camera_py;
        public string image;
        public double timestamp;
        public string version;
    }

    [Serializable]
    public class LocalizationRequestwithToken
    {
        public string developer_token;
        public List<int> cloud_Ids;
        public int width;
        public int height;
        public int channel;
        public float Camera_fx;
        public float Camera_fy;
        public float Camera_px;
        public float Camera_py;
        public string image;
        public double timestamp;
        public string version;
    }

    [Serializable]
    public class LocalizationResponse
    {
        public bool poseAvailable;
        public int counter;
        public float[] pose;
        public int cloudid;
        public string isTrackMonocular;
        public string message;

    }


    [Serializable]
    public class MapListItem
    {
        public map_list[] map_list;

    }

    [Serializable]
    public class map_list
    {
        public string map_id;
        public string map_name;
        public string map_image;
        public string map_address;
        public map_location mapLocation;
    }

    [Serializable]
    public class map_location
    {
        public float latitude;
        public float longitude;
    }



    [Serializable]
    public class MapAssetData
    {
        public Waypoints[] Waypoints;
        public Destinations[] Destinations;
        public FloorPlans[] FloorPlans;
        public FloorImages[] FloorImages;
        public GlbModels[] GlbModels;
        public Texts[] Texts;

        public CloudMaps[] CloudMaps;

    }


    [Serializable]
    public class Waypoints
    {
        public string id;
        public string name;
        public Position Position;
        public Rotation Rotation;
        public Scale Scale;
    }

    [Serializable]
    public class Destinations
    {
        public string id;
        public string name;
        public string link;
        public Position Position;
        public Rotation Rotation;
        public Scale Scale;
        public string description;
    }

    [Serializable]
    public class FloorPlans
    {
        public string id;
        public string name;
        public string link;
        public Position Position;
        public Rotation Rotation;
        public Scale Scale;
    }

    [Serializable]
    public class FloorImages
    {
        public string id;
        public string name;
        public string link;
        public Position Position;
        public Rotation Rotation;
        public Scale Scale;
    }

    [Serializable]
    public class GlbModels
    {
        public string id;
        public string name;
        public string link;
        public Position Position;
        public Rotation Rotation;
        public Scale Scale;
    }

    [Serializable]
    public class Texts
    {
        public string id;
        public string name;
        public string link;
        public Position Position;
        public Rotation Rotation;
        public Scale Scale;
    }

    [Serializable]
    public class CloudMaps
    {
        public string id;
        public int pcd_id;
        public string name;
        public string link;
        public Position Position;
        public Rotation Rotation;
        public Scale Scale;
        public string anchorid;
    }

    [Serializable]
    public class Position
    {
        public double posX;
        public double posY;
        public double posZ;
    }

    [Serializable]
    public class Rotation
    {
        public double rotX;
        public double rotY;
        public double rotZ;
    }

    [Serializable]
    public class Scale
    {
        public double scaX;
        public double scaY;
        public double scaZ;
    }

    [Serializable]
    public class CloudListItem
    {
        public CloudMapList[] cloudMapList;

    }

    [Serializable]
    public class CloudMapList
    {
        public string id;
        public string map_name;
        public string binary_link;
        public string pcd_link;
        public string uploaded;
        public string AnchorId;
        public string Latitude;
        public string Longitude;
    }


    [Serializable]
    public class CloudMapOffset
    {
        public Vector3 position;
        public Quaternion rotation;

    }


    [Serializable]
    public class ArCameraOffset
    {
        public Vector3 position;
        public Quaternion rotation;

    }

    [Serializable]
    public struct MapRequest
    {
        public string devToken;
        public string map_name;

        public string latitude;
        public string longitude;
        public string altitude;

        public string pcdPath;
        public string version;

        public string anchorId;
    }

}

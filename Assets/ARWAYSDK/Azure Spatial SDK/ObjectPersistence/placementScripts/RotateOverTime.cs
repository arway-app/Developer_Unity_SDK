/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using UnityEngine;

public class RotateOverTime : MonoBehaviour
{
    [SerializeField]
    private Vector3 m_Axis = new Vector3(0f, 1, 0f);
    [SerializeField]
    private float m_Speed = 30f;

    void Update()
    {
        transform.Rotate(m_Axis, m_Speed * Time.deltaTime);
    }
}

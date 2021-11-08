using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    public Vector3 Speed;
    void Update()
    {
        transform.rotation *= Quaternion.Euler(Speed * Time.deltaTime);
    }
}

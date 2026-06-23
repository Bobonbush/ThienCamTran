using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class CameraCustomBound : MonoBehaviour
{ 

    [SerializeField] 
    public Vector3 localPosition = Vector3.zero;


    [SerializeField]
    private bool _customBoundaries = false;


    public bool customBoundaries
    {
        get {  return _customBoundaries; }
        private set { _customBoundaries = value; }
    }
    public Collider2D Boundaries;

}


using System.Collections.Generic;
using System.Text;
using UnityEngine;

public abstract class ResuableObject : MonoBehaviour, IReusable
{
    public abstract void OnSpawn();


    public abstract void OnDespawn();

}

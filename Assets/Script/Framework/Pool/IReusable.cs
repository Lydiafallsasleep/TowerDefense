using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IReusable
{
    // take object from pool
    void OnSpawn();
    // return object to pool
    void OnDespawn();
}

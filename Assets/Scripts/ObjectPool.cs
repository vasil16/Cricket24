using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool instance;
    public List<ObjectPoolData> pooledObjects;

    private void Awake()
    {
        instance = this;
        pooledObjects = new List<ObjectPoolData>();
    }

    public GameObject GetPooledObjects(GameObject game)
    {
        GameObject obj;
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (pooledObjects[i].gameObject == game)
            {
                obj = pooledObjects[i].getObject();
                if (obj != null)
                {
                    return obj;
                }
                else
                { 
                    obj = CreateObject(game);
                    pooledObjects[i].objects.Add(obj);
                    return obj;
                }
            }

        }
        ObjectPoolData poolData = new ObjectPoolData();
        poolData.gameObject = game;
        obj = CreateObject(game);
        poolData.objects.Add(obj);
        pooledObjects.Add(poolData);
        return obj;
    }

    public GameObject CreateObject(GameObject objToCeate)
    {
        GameObject gameObj = Instantiate(objToCeate);
        gameObj.SetActive(false);
        return gameObj;
    }

    public void DeactivateAll(GameObject game)
    {
        for(int k = 0; k < pooledObjects.Count; k++)
        {
            if (pooledObjects[k].gameObject == game)
            {
                for(int l = 0; l < pooledObjects[k].objects.Count; l++)
                {
                    pooledObjects[k].objects[l].SetActive(false);
                }
            }
        }
    }

    public void ClearPool()
    {
       
        for (int k = 0; k < pooledObjects.Count; k++)
        {
            for (int j = 0; j < pooledObjects[k].objects.Count;j++)
            {
                Destroy(pooledObjects[k].objects[j]);
            }
            
               
        }
    }
}

[System.Serializable]
public class ObjectPoolData
{
    public GameObject gameObject;
    public List<GameObject> objects;

    public ObjectPoolData()
    {
        objects = new List<GameObject>();
    }

    public GameObject getObject()
    {
        for (int j = 0; j < objects.Count; j++)
        {
            if (!objects[j].activeInHierarchy)
            {
                return objects[j];
            }
        }
        return null;
    }

   
}

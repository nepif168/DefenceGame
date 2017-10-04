using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour  {

    static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = (T)FindObjectOfType(typeof(T));

                if (instance == null)
                    Debug.LogError(typeof(T) + "が存在しません");
            }

            return instance;
        }
    }
}
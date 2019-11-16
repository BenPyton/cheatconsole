using UnityEngine;

public class Test : MonoBehaviour
{
    private void Awake()
    {
        CheatConsole.OnOpen.AddListener(() => Debug.Log("Console opened"));
        CheatConsole.OnClose.AddListener(() => Debug.Log("Console closed"));
    }

    [Cheat]
    private static void CheatMethod(int test)
    {
        CheatConsole.Log("Hello " + test);
    }
}

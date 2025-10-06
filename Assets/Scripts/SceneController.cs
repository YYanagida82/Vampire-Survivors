using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void SceneChange(string name)
    {
        if (SoundManager.Instance != null && SoundManager.Instance.bgmSource.isPlaying)
        {
            SoundManager.Instance.StopBGM(0f);
        }
        SceneManager.LoadScene(name);
        Time.timeScale = 1f; // 時間停止解除
    }
}

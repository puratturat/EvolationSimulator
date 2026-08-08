using UnityEngine;

public class TimeManager : MonoBehaviour
{
    // Klavyeden gelen tuş basımlarını her saniye kontrol ettiğimiz döngü
    void Update()
    {
        // 0 Tuşuna basıldığında (Oyunu Durdur)
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Space))
        {
            PauseGame();
        }
        // 1 Tuşuna basıldığında (Normal Hız)
        else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            NormalSpeed();
        }
        // 2 Tuşuna basıldığında (2x Hız)
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SpeedUp2x();
        }
        // 3 Tuşuna basıldığında (4x Hız)
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SpeedUp4x();
        }
        // 4 Tuşuna basıldığında (8x Hız)
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            SpeedUp8x();
        }
        // 5 Tuşuna basıldığında (16x Hız)
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            SpeedUp16x();
        }
    }

    // --- BUTONLARIN VE TUŞLARIN ÇAĞIRDIĞI FONKSİYONLAR ---

    // 0. Oyunu Durdur (Pause)
    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    // 1. Normal Hız (1x)
    public void NormalSpeed()
    {
        Time.timeScale = 1f;
    }

    // 2. 2x Hızlandır
    public void SpeedUp2x()
    {
        Time.timeScale = 2f;
    }

    // 3. 4x Hızlandır
    public void SpeedUp4x()
    {
        Time.timeScale = 4f;
    }

    // 4. 8x Hızlandır
    public void SpeedUp8x()
    {
        Time.timeScale = 8f;
    }

    // 5. 16x Hızlandır (YENİ)
    public void SpeedUp16x()
    {
        Time.timeScale = 16f;
    }
}
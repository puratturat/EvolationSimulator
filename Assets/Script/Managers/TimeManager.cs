using UnityEngine;
using TMPro;

public class TimeManager : MonoBehaviour
{
    [Header("Simülasyon Bilgisi")]
    [SerializeField] private TextMeshProUGUI simulationInfoText;

    private float elapsedSimulationTime;
    private int lastDisplayedSecond = -1;

    // Klavyeden gelen tuş basımlarını her saniye kontrol ettiğimiz döngü
    void Update()
    {
        elapsedSimulationTime += Time.deltaTime;
        UpdateSimulationInfo();

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

    private void UpdateSimulationInfo()
    {
        if (simulationInfoText == null)
        {
            return;
        }

        int totalSeconds = Mathf.FloorToInt(elapsedSimulationTime);
        if (totalSeconds == lastDisplayedSecond)
        {
            return;
        }

        lastDisplayedSecond = totalSeconds;
        int hours = totalSeconds / 3600;
        int minutes = totalSeconds / 60 % 60;
        int seconds = totalSeconds % 60;
        simulationInfoText.text = $"Geçen Süre: {hours:00}:{minutes:00}:{seconds:00}  •  v{Application.version}";
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

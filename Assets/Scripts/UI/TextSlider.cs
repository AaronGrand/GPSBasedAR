using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TextSlider : MonoBehaviour
{
    public TextMeshProUGUI sliderValueText;
    public Slider slider;

    void Update()
    {
        float areaInSquareMeters = slider.value * slider.value;

        float areaInSquareKilometers = areaInSquareMeters / 1_000_000;

        sliderValueText.text = areaInSquareKilometers.ToString("0.00") + " km²";
    }
}

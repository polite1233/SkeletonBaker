using System;
using System.Collections;
using System.Collections.Generic;
using com.skeleton.Baker.GPUAnimate;
using GPUAnimate;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ShowClipButton : MonoBehaviour
{
    public Button temp;

    private void CreateBtn(UnityAction click, string name = "Btn")
    {
        var instantiate = Instantiate(temp, transform);
        instantiate.GetComponentInChildren<Text>().text = name;
        instantiate.onClick.AddListener(click);
        instantiate.gameObject.SetActive(true);
    }

    private void DestoryAllBtn()
    {
        Button[] buttons = GetComponentsInChildren<Button>();
        foreach (var btn in buttons)
        {
            if (btn.IsActive())
            {
                Destroy(btn);
            }
        }
    }

    // Start is called before the first frame update
    public void ShowClipBtn(AnimateLoader.AnimateData animateData, AnimateLoader loader)
    {
        int index = 0;
        foreach (var data in animateData.textureData.clipDatas)
        {
            int a = index;
            CreateBtn(delegate { loader.SetClipIndex(a); }, data.name);
            index++;
        }
    }
}
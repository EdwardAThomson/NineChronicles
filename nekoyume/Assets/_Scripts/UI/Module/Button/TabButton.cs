﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.Game.Controller;

namespace Nekoyume.UI.Module
{
    public class TabButton : MonoBehaviour, IToggleable
    {
        public Button button;
        public GameObject disabledContent;
        public Image disabledImage;
        public GameObject enabledContent;
        public Image enabledImage;

        #region IToggleable

        private IToggleListener _toggleListener;

        public string Name => name;

        public bool IsToggleable { get; set; }

        public virtual bool IsToggledOn => enabledContent.activeSelf;

        public void SetToggledOff()
        {
            disabledContent.SetActive(true);
            enabledContent.SetActive(false);
        }

        public void SetToggledOn()
        {
            disabledContent.SetActive(false);
            enabledContent.SetActive(true);
        }

        public void SetToggleListener(IToggleListener toggleListener)
        {
            _toggleListener = toggleListener;
        }

        #endregion

        private void Awake()
        {
            button.onClick.AddListener(SubscribeOnClick);
        }

        private void SubscribeOnClick()
        {
            if (IsToggledOn)
                return;

            AudioController.PlayClick();
            _toggleListener?.OnToggle(this);
        }
    }
}
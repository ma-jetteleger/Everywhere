using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputTypeOption : MonoBehaviour
{
	[SerializeField] private Image _outline = null;
	[SerializeField] private Image _check = null;

	public Image Outline => _outline;
	public Image Check => _check;
}

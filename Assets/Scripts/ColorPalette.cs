using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorPalette : MonoBehaviour
{
	[SerializeField] private Image _outline = null;
	[SerializeField] private Image _check = null;
	[SerializeField] private Image[] _coloredNodes = null;

	public Image Outline => _outline;
	public Image Check => _check;
	public Image[] ColoredNodes => _coloredNodes;
}

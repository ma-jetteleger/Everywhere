using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ColorBlock : MonoBehaviour
{
	[SerializeField] private Image[] _colorSlices = null;
	[SerializeField] private Vector2 _widthRange = Vector2.zero;

	public void Shuffle(Color[] colors)
	{
		var random = new System.Random();
		
		for (var i = 0; i < colors.Length; i++)
		{
			var j = UnityEngine.Random.Range(i, colors.Length);

			var temp = colors[i];
			colors[i] = colors[j];
			colors[j] = temp;
		}

		for (var i = 0; i < _colorSlices.Length; i++)
		{
			_colorSlices[i].color = colors[i];
			_colorSlices[i].GetComponent<LayoutElement>().flexibleWidth = UnityEngine.Random.Range(_widthRange.x, _widthRange.y);
		}

		transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 1f) > 0.5f ? -45f : 45f);
	}
}

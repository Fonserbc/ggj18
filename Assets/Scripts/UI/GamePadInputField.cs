using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GamePadInputField : InputField
{
	protected override void LateUpdate()
	{
		base.LateUpdate();

		if (Input.GetButtonDown("Cancel"))
		{
			OnDeselect(null);
			m_OnCancel.Invoke();
		}
	}

	[Serializable]
	public class CancelEvent : UnityEvent { }

	// Event delegates triggered on click.
	[FormerlySerializedAs("onCancel")]
	[SerializeField]
	private CancelEvent m_OnCancel = new CancelEvent();

	public CancelEvent onCancel
	{
		get { return m_OnCancel; }
		set { m_OnCancel = value; }
	}

	public void OnCancel(BaseEventData eventData)
	{
		m_OnCancel.Invoke();
	}
}

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class CancelEventHandler : MonoBehaviour, ICancelHandler
{
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

	private void Cancel()
	{
		m_OnCancel.Invoke();
	}

	public void OnCancel(BaseEventData eventData)
	{
		Cancel();
	}
}

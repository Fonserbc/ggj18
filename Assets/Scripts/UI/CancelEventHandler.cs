using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class CancelEventHandler : MonoBehaviour, ICancelHandler
{
	public Color Unselected;
	public Color Selected;
	public Graphic TargetGraphic;

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

	public void Update()
	{
		if (TargetGraphic != null)
		{
			TargetGraphic.color = EventSystem.current.currentSelectedGameObject == gameObject ? Selected : Unselected;
		}
	}
}

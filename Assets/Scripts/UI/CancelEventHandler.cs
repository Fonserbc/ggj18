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

	public HorizontalOrVerticalLayoutGroup GroupLayout;
	public RectOffset SelectedOffset;

	private RectTransform rectTransform;

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

	public void OnEnable()
	{
		if (GroupLayout != null)
		{
			RectOffset curr = GroupLayout.padding;
			curr.left = curr.right = curr.bottom = curr.top = 0;
			rectTransform = GetComponent<RectTransform>();
		}
	}

	public void Update()
	{
		bool selected = EventSystem.current.currentSelectedGameObject == gameObject;

		if (TargetGraphic != null)
		{
			TargetGraphic.color = selected ? Selected : Unselected;
		}

		if (GroupLayout != null)
		{
			RectOffset curr = GroupLayout.padding;
			curr.left = (int)((selected ? SelectedOffset.left : 0) * 0.3f + curr.left * 0.7f);
			curr.right = (int)((selected ? SelectedOffset.right : 0) * 0.3f + curr.right * 0.7f);
			curr.top = (int)((selected ? SelectedOffset.top : 0) * 0.3f + curr.top * 0.7f);
			curr.bottom = (int)((selected ? SelectedOffset.bottom : 0) * 0.3f + curr.bottom * 0.7f);

			LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
		}
	}
}

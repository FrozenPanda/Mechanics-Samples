using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Systems.WeeklyEventSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Pathfinding;

public class InteractionExpendable : Expendable
{
    private Interactable[] interactionObjects;
	private bool isShowingIndicator;

	private float tileUIEnlargeRatio;
	private float tileUIEnlargeTime;

	protected override void Initialize()
    {
		tileUIEnlargeRatio = ConfigurationService.Configurations.TileUIEnlargeRatio;
		tileUIEnlargeTime = ConfigurationService.Configurations.TileUIEnlargeTime;
		interactionObjects = unlockedPart.GetComponentsInChildren<Interactable>();
        base.Initialize();
    }

    public override bool LoadInteraction(IInteractor _interactor)
    {
		if (!isCharacterWaiting)
        {
            isCharacterWaiting = true;
            characterWaitStartTime = Time.time;
        }

		bool hasCoins = cost == 0 || ExchangeService.GetValue(CurrencyService.ActiveCurrencyType) > 0;
		bool isCharacterSaler = _interactor.CharacterType == CharacterType.MainCharacter;
		bool requirementsMet = (RequiredObjects == null || RequiredObjects.Count <= 0 || ExpendManager.Instance.IsAllExpended(RequiredObjects));

		if (!isExpended && lockedPart.activeSelf && (hasCoins || unlocksWithAd) && isCharacterSaler && requirementsMet)
        {
			isBroken = false;
            return base.LoadInteraction(_interactor);
        }
		else
        {
			return false;
        }
    }

    public override void SetState(bool _isExpended, bool isAnimated = false)
    {
        base.SetState(_isExpended, isAnimated);
        if (isAnimated)
        {
            if (interactionObjects != null && interactionObjects.Length > 0)
            {
                foreach (var interactable in interactionObjects)
                {
                    interactable.gameObject.SetActive(false);
                    interactable.gameObject.transform.DOScale(Vector3.zero, 0.001f);
                }
            }
			Sequence sq = DOTween.Sequence();
			sq.Append(unlockedPart.transform.DOScale(Vector3.zero, 0.001f));
			sq.AppendCallback(() => unlockedPart.SetActive(showAlwaysLockedPart || _isExpended));
			sq.Append(unlockedPart.transform.DOScale(Vector3.one, 1f).SetEase(Ease.InOutElastic));
			sq.AppendCallback(() =>
			{
				ParticleSystem particle = null;
				particle = ParticleManager.Instance.PlayParticle(PoolType.NewTileParticle, Vector3.zero, unlockedPart.transform);
				particle.transform.localPosition = new Vector3(0f, 0.5f, 0f);
				StartCoroutine(DestroyParticle(particle.gameObject));

				if (interactionObjects.Length > 0)
				{
					foreach (var interactable in interactionObjects)
					{
						if (interactable != null)
						{
							interactable.gameObject.SetActive(true);
							//InteractionManager.Instance.UpdateAllContainersIcon();
							interactable.gameObject.transform.DOScale(Vector3.one, 1f).SetEase(Ease.InOutElastic).OnComplete(() =>
							{
								UpdateNavmeshCutters();
								//SpawnTileIcon();
								Expend();
							});
						}
						else
						{
							//SpawnTileIcon();
							Expend();
						}
					}
				}

				UpdateNavmeshCutters();
			});
        }
    }

    public override void ShowCapacity()
    {
        if (!isShowingIndicator)
        {
			isShowingIndicator = true;
			lockedPart.transform.DOScale(Vector3.one + (Vector3.one * tileUIEnlargeRatio), tileUIEnlargeTime);
        }
    }

    public override void HideCapacity()
    {
		if (isShowingIndicator)
		{
			isShowingIndicator = false;
			lockedPart.transform.DOScale(Vector3.one, tileUIEnlargeTime);
		}
	}
}

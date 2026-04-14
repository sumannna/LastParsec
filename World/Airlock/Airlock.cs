using System.Collections;
using UnityEngine;

/// <summary>
/// エアロック本体。与圧/減圧シーケンスとハッチの安全インターロックを管理する。
/// </summary>
public class Airlock : MonoBehaviour
{
    public enum State
    {
        Pressurized,    // 与圧（空気あり）
        Depressurized,  // 減圧（空気なし）
        Cycling,        // 移行中（操作不可）
    }

    [Header("設定")]
    [SerializeField] private float airlockVolume = 5f;
    [SerializeField] private float cyclingDuration = 3f;

    [Header("ハッチ参照")]
    [SerializeField] private AirlockHatch innerHatch;
    [SerializeField] private AirlockHatch outerHatch;

    [Header("ゾーン参照")]
    [SerializeField] private AirlockZone airlockZone;

    public State CurrentState { get; private set; } = State.Pressurized;

    // -----------------------------------------------
    // 外部からのリクエスト
    // -----------------------------------------------

    public void RequestPressurize()
    {
        if (CurrentState != State.Depressurized) return;
        StartCoroutine(PressurizeSequence());
    }

    public void RequestDepressurize()
    {
        if (CurrentState != State.Pressurized) return;
        StartCoroutine(DepressurizeSequence());
    }

    // -----------------------------------------------
    // シーケンス：空気を入れる（Depressurized → Pressurized）
    // -----------------------------------------------

    IEnumerator PressurizeSequence()
    {
        CurrentState = State.Cycling;
        Debug.Log("[Airlock] 与圧シーケンス開始");

        // 1. 船外側ハッチを閉じる
        bool outerClosed = false;
        if (outerHatch != null && outerHatch.IsOpen)
            outerHatch.Close(() => outerClosed = true);
        else
            outerClosed = true;

        yield return new WaitUntil(() => outerClosed && (outerHatch == null || !outerHatch.IsMoving));

        // 2. 船内酸素を消費してエアロック内に充填
        bool charged = false;
        if (ShipAtmosphereSystem.Instance != null)
            charged = ShipAtmosphereSystem.Instance.ConsumeForAirlock(airlockVolume);
        else
            charged = true;

        if (!charged)
        {
            Debug.Log("[Airlock] 与圧失敗：船内酸素不足");
            CurrentState = State.Depressurized;
            airlockZone?.Refresh();
            yield break;
        }

        yield return new WaitForSeconds(cyclingDuration);

        // 3. 与圧完了
        CurrentState = State.Pressurized;
        airlockZone?.Refresh();
        Debug.Log("[Airlock] 与圧完了");

        // 4. 船内側ハッチを開く
        innerHatch?.Open();
    }

    // -----------------------------------------------
    // シーケンス：空気を抜く（Pressurized → Depressurized）
    // -----------------------------------------------

    IEnumerator DepressurizeSequence()
    {
        CurrentState = State.Cycling;
        Debug.Log("[Airlock] 減圧シーケンス開始");

        // 1. 船内側ハッチを閉じる
        bool innerClosed = false;
        if (innerHatch != null && innerHatch.IsOpen)
            innerHatch.Close(() => innerClosed = true);
        else
            innerClosed = true;

        yield return new WaitUntil(() => innerClosed && (innerHatch == null || !innerHatch.IsMoving));

        // 2. エアロック内の空気を宇宙に廃棄（ShipAtmosphereには戻らない）
        yield return new WaitForSeconds(cyclingDuration);

        // 3. 減圧完了
        CurrentState = State.Depressurized;
        airlockZone?.Refresh();
        Debug.Log("[Airlock] 減圧完了");

        // 4. 船外側ハッチを開く
        outerHatch?.Open();
    }
}
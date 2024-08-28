using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NRKernal;

public class DrawManager : MonoBehaviour
{
    // InspectorでLineObjectを設定する
    [SerializeField] GameObject LineObject;

    // 描画中のLineObject
    private GameObject CurrentLineObject = null;

    // 色のリスト
    private List<Color> colors = new List<Color> { Color.red, Color.green, Color.blue, Color.yellow };
    private int currentColorIndex = 0;

    // ピンチ状態を追跡するためのフラグ
    private bool isPinching = false;

    // 使用中の手を記録する変数
    private HandEnum? activeHand = null;

    // Use this for initialization
    void Start() {}

    // Update is called once per frame
    void Update()
    {
        // 左手と右手のトラッキング状態を取得
        HandState handState_l = NRInput.Hands.GetHandState(HandEnum.LeftHand);
        HandState handState_r = NRInput.Hands.GetHandState(HandEnum.RightHand);

        // トラッキングされている手を最初に検出
        if (activeHand == null)
        {
            if (handState_l.isTracked)
            {
                activeHand = HandEnum.LeftHand;
                Debug.Log("Left hand detected first. Using left hand for drawing.");
            }
            else if (handState_r.isTracked)
            {
                activeHand = HandEnum.RightHand;
                Debug.Log("Right hand detected first. Using right hand for drawing.");
            }
        }

        // トラッキングされている手がない場合は処理を中止
        if (activeHand == null)
        {
            Debug.Log("No hands are being tracked.");
            return;
        }

        // アクティブな手のHandStateを取得
        HandState handState = NRInput.Hands.GetHandState((HandEnum)activeHand);

        // 手がトラッキングされていない場合は処理を中止
        if (!handState.isTracked)
        {
            Debug.Log((activeHand == HandEnum.LeftHand ? "Left" : "Right") + " hand lost tracking.");
            activeHand = null; // 手が見えなくなったら再び手の検出を行う
            return;
        }

        // 人差し指の先端のPoseを取得
        Pose indexTipPose = handState.GetJointPose(HandJointID.IndexTip);
        // 親指の先端のPoseを取得
        Pose thumbTipPose = handState.GetJointPose(HandJointID.ThumbTip);

        // 親指と人差し指の距離を計算
        float distance = Vector3.Distance(indexTipPose.position, thumbTipPose.position);

        // ピンチポーズを検出（例えば、親指と人差し指が近いときにピンチとみなす）
        bool isCurrentlyPinching = distance < 0.03f;  // 適切な距離を設定（ここでは3cm）

        // ピンチポーズが開始されたときに色を変更
        if (isCurrentlyPinching && !isPinching)
        {
            currentColorIndex = (currentColorIndex + 1) % colors.Count;
            Debug.Log("Color changed to: " + colors[currentColorIndex].ToString());
        }

        // 人差し指がポイントしているかチェック
        if (handState.isPointing)
        {
            if (CurrentLineObject == null)
            {
                // LineObjectを生成
                CurrentLineObject = Instantiate(LineObject, Vector3.zero, Quaternion.identity);

                // 初回描画時に色を設定
                LineRenderer render = CurrentLineObject.GetComponent<LineRenderer>();
                render.startColor = colors[currentColorIndex];
                render.endColor = colors[currentColorIndex];
            }

            // ゲームオブジェクトからLineRendererコンポーネントを取得
            LineRenderer lineRenderer = CurrentLineObject.GetComponent<LineRenderer>();

            // LineRendererからPositionsのサイズを取得
            int nextPositionIndex = lineRenderer.positionCount;

            // LineRendererのPositionsのサイズを増やす
            lineRenderer.positionCount = nextPositionIndex + 1;

            // LineRendererのPositionsに人差し指の先端の位置情報を追加
            lineRenderer.SetPosition(nextPositionIndex, indexTipPose.position);
        }
        else // ポイントポーズ以外のとき
        {
            // 描画中の線を終了
            if (CurrentLineObject != null)
            {
                CurrentLineObject = null;
            }
        }

        // ピンチ状態を更新
        isPinching = isCurrentlyPinching;
    }
}

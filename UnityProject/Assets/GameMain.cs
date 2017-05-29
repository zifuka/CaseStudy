﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct FIELD
{
    public bool Alive;
    public bool Break;
    public Vector2 Pos;
    public GameObject Cube;
};

public enum PHASE
{
    STAY = 0,
    PUSH,
    SERACH,
    VANISH,
    SLIDE,
    DROP,
    GENERATE,
};


public enum CountType
{
    Y_Count = 0,
    X_Count,
};

public class GameMain : MonoBehaviour {

    //検索対象のマテリアル名
    public string[] CubeMats = { "Red", "Blue", "Green", "Yellow", "Purple" };
    // 

    public int gridWidth;       //オブジェクトの横配列量
    public int gridHeight;      //オブジェクトの縦配列量
    public GameObject Prefab;   //配置するオブジェク
    public GameObject NextBlock;//UI用の次のブロック
    public GameObject VanishEffect; //ブロック破壊時に出すエフェクト

    //更新周期
    public float span;

    //ゲームのフェイズ
    public PHASE Phase;

    //GameObject[,] Blocks = new GameObject[5, 10];

    //フレームの変数
    float time = 0.0f;

    //ゲームのフィールド管理用配列
    public FIELD[,] Field = new FIELD[7,7];

    //ブロックの連立数を計測するための変数
    int BlockCounter = 0;

    //連鎖を起こしたい
    bool VanishCaller = false;

    //各処理を一回のみ処理させ、状態遷移まで待機にするフラグ
    bool Action = false;

    //差し込み用ブロックの待機列
    private string[] NextBlocks = new string[4];

    //タップした場所の変数入れ
    private int TapPoint_X;
    private int TapPoint_Y;

    void Start()
    {
        Phase = PHASE.STAY;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                //Debug.Log(x);
                Field[x, y].Pos = new Vector2(x - (gridWidth / 2), y);

                //GameObject g = Instantiate(Prefab, new Vector3(x - (gridWidth / 2), y + 1, 0), Quaternion.identity) as GameObject;

                GameObject g = RandColorCreateBlock(new Vector3(x - (gridWidth / 2), y + 1, 0));

                //生成したオブジェクとの親にこのオブジェクトを設定
                g.transform.parent = gameObject.transform;

                SetCubeData(x, y, g);
            }
        }

        for(int i = 0;i < 4; i++)
        {
            NextBlocks[i] = CubeMats[Random.Range(0, CubeMats.Length)];
        }
	}

    // Update is called once per frame
    void Update()
    {
        //int Cube_Cnt = 0;
        int x, y;
   
        switch (Phase)
        {
            //待機
            case PHASE.STAY:
                //挿入用ブロックにマテリアルを設定
                NextBlock.GetComponent<Renderer>().material = Resources.Load("Materials/" + NextBlocks[0]) as Material;

                //左クリックされたら
                if (Input.GetMouseButtonDown(0))
                {
                    var tapPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    var collition2d = Physics2D.OverlapPoint(tapPoint);

                    //２Dのあたり判定に重なっていたら
                    if (collition2d)
                    {
                        //レイを飛ばして当たったオブジェクトがあるなら
                        var hit = Physics2D.Raycast(tapPoint, -Vector2.up);
                        if (hit)
                        {
                            int Col_X = (int)hit.collider.gameObject.transform.position.x + 3;
                            int Col_Y = Mathf.RoundToInt(hit.collider.gameObject.transform.position.y);

                            //オブジェクトが配置されている配列のXとY
                            TapPoint_X = Col_X;
                            TapPoint_Y = Col_Y;
                        }
                    }

                    Debug.Log("クリック！");
                    Phase = PHASE.PUSH;
                }
                break;
            //差し込み時のスライド処理
            case PHASE.PUSH:
                FIELD SlideWork;
                FIELD ClashWork;

                Rigidbody2D Rigid;
                
                if (TapPoint_X + 1 < gridWidth)
                {
                    if (TapPoint_Y + 1 < gridHeight)
                    {
                        for (int i = 0; i < gridWidth; i++)
                        {
                            //挿入するブロックの上のラインがあるのなら動けなくする
                            Rigid = Field[i, TapPoint_Y + 1].Cube.GetComponent<Rigidbody2D>();
                            Rigid.constraints = RigidbodyConstraints2D.FreezePositionY;
                        }
                    }

                    SlideWork = Field[(int)TapPoint_X, (int)TapPoint_Y];

                    for (int i = TapPoint_X + 1;i < gridWidth; i++)
                    {
                        //右隣りのブロック情報をコピー
                        ClashWork = Field[i, TapPoint_Y];
                        //書き換え
                        Field[i, TapPoint_Y] = SlideWork;

                        Field[i, TapPoint_Y].Cube.GetComponent<Transform>().localPosition += new Vector3(1,0,0);

                        SlideWork = ClashWork;
                    }

                    Rigid = SlideWork.Cube.GetComponent<Rigidbody2D>();
                    Rigid.constraints = RigidbodyConstraints2D.None;
                    Rigid.AddForce(new Vector2(100,0));
                    SlideWork.Cube.GetComponent<Transform>().localPosition += new Vector3(0.9f,0,0);
                    SlideWork.Cube.GetComponent<Transform>().localScale = new Vector3(0.9f, 0.9f, 0.5f);
                    Destroy(SlideWork.Cube,1);

                    //挿入用オブジェクトのインスタンス化
                    GameObject g = Instantiate(Prefab, new Vector3(TapPoint_X - (gridWidth / 2), TapPoint_Y, 0), Quaternion.identity) as GameObject;

                    //生成したオブジェクとの親にこのオブジェクトを設定
                    g.transform.parent = gameObject.transform;

                    //Fieldにデータをセット
                    SetCubeData(TapPoint_X, TapPoint_Y, g);

                    if (TapPoint_Y + 1 < gridHeight)
                    {
                        for (int i = 0; i < gridWidth; i++)
                        {
                            //動けなくしていた部分をもとに戻す
                            Rigid = Field[i, TapPoint_Y + 1].Cube.GetComponent<Rigidbody2D>();
                            //解除
                            Rigid.constraints = RigidbodyConstraints2D.None;
                            //再設定
                            Rigid.constraints = RigidbodyConstraints2D.FreezePositionX;
                            Rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
                        }
                    }
                }

                Field[(int)TapPoint_X, (int)TapPoint_Y].Cube.GetComponent<Renderer>().material = Resources.Load("Materials/" + NextBlocks[0]) as Material;
                Field[(int)TapPoint_X, (int)TapPoint_Y].Cube.GetComponent<Block>().CubeName = NextBlocks[0];

                Debug.Log("X:" + TapPoint_X + "Y:" + TapPoint_Y);
                Debug.Log("挿入したブロック:" + Field[TapPoint_X, TapPoint_Y].Cube.GetComponent<Block>().CubeName);

                //挿入待ちブロックの色を更新
                NextBlocks[0] = NextBlocks[1];

                //配列をずらす
                for (int i = 1;i < 3;i++)
                {
                    NextBlocks[i] = NextBlocks[i + 1];
                }

                //最後尾にランダムでカラーの名前を入れておく
                NextBlocks[3] = CubeMats[Random.Range(0, CubeMats.Length)];

                Phase = PHASE.SERACH;
                break;
            //ブロックが連なりを検出する処理
            case PHASE.SERACH:
                Debug.Log("サーチフェイズ");

                Debug.Log("X:" + TapPoint_X + "Y:" + TapPoint_Y);
                Debug.Log("挿入したブロック:" + Field[TapPoint_X, TapPoint_Y].Cube.GetComponent<Block>().CubeName);

                VanishCaller = false;

                for (x = 0; x < gridWidth; x++)
                {
                    for (y = 0; y < gridHeight; y++)
                    {
                        //横方向に連続しているか
                        BlockCounter = 0;
                        CountBlocks(x, y, CountType.X_Count);

                        //縦方向に連続しているか
                        BlockCounter = 0;
                        CountBlocks(x, y, CountType.Y_Count); 
                    }
                }
                Phase = PHASE.VANISH;
                break;
            //ブロックの消去処理
            case PHASE.VANISH:
                Debug.Log("ヴァニッシュフェイズ");
                if (Action == false)
                {
                    for (x = 0; x < gridWidth; x++)
                    {
                        for (y = 0; y < gridHeight; y++)
                        {
                            if (Field[x, y].Break == true)
                            {
                                GameObject g = Instantiate(VanishEffect, new Vector3(x - 3, y, -1), Quaternion.identity) as GameObject;

                                var Ps = g.GetComponent<ParticleSystem>();
                                Ps.GetComponent<Renderer>().material = Field[x, y].Cube.GetComponent<Renderer>().material;

                                Destroy(Field[x, y].Cube, span);
                                Field[x, y].Alive = false;
                                Field[x, y].Break = false;

                                VanishCaller = true;

                                //Debug.Log("X" + x + "Y" + y);
                            }
                        }
                    }
                    Action = true;
                }

                time += Time.deltaTime;

                if (time > span * 2)
                {
                    time = 0;

                    if (VanishCaller == true)
                    {
                        Phase = PHASE.DROP;
                        Action = false;
                    }
                    else
                    {
                        Phase = PHASE.STAY;
                        Action = false;
                    }
                }
                
                break;
            //ブロックのずらし処理
            case PHASE.SLIDE:
                int SlideCnt = 0;
                int SlideEmpCnt = 0;
                if (Action == false)
                {
                    for (y = 0; y < gridHeight; y++)
                    {
                        //左半分
                        for (x = 0; x < gridHeight / 3; x++)
                        {
                            //フィールドに空きを見つけた
                            if (Field[x, y].Alive == false)
                            {
                                SlideCnt++;
                                //空き領域より上にブロックが存在するか探す
                                for (SlideEmpCnt = x + 1; SlideEmpCnt < gridWidth / 3; SlideEmpCnt++)
                                {
                                    if (Field[x, SlideEmpCnt].Alive == true)
                                    {
                                        break;
                                    }
                                    //空き領域までの数をカウント
                                    SlideCnt++;
                                }
                                //存在しない場合はカウントが不必要になるので0へ
                                if (SlideEmpCnt == gridHeight)
                                {
                                    SlideCnt = 0;
                                }

                                if (SlideCnt > 0)
                                {
                                    for (int i = SlideEmpCnt; SlideEmpCnt < gridWidth; SlideEmpCnt++)
                                    {
                                        //Debug.Log("移動してる行：" + EmpCnt + "下がった量:" + DropCnt);
                                        Field[SlideEmpCnt - SlideCnt, y] = Field[SlideEmpCnt, y];
                                        Field[SlideEmpCnt, y].Cube = null;
                                        Field[SlideEmpCnt, y].Alive = false;
                                    }
                                    SlideCnt = 0;
                                }
                            }
                        }

                        SlideCnt = 0;

                        //右半分
                        for (x = gridWidth - 1; x > gridWidth / 3; x--)
                        {
                            //フィールドに空きを見つけた
                            if (Field[x, y].Alive == false)
                            {
                                SlideCnt++;
                                //空き領域より上にブロックが存在するか探す
                                for (SlideEmpCnt = x - 1; SlideEmpCnt > gridWidth / 3; SlideEmpCnt--)
                                {
                                    if (Field[x, SlideEmpCnt].Alive == true)
                                    {
                                        break;
                                    }
                                    //空き領域までの数をカウント
                                    SlideCnt++;
                                }
                                //存在しない場合はカウントが不必要になるので0へ
                                if (SlideEmpCnt == gridHeight)
                                {
                                    SlideCnt = 0;
                                }

                                if (SlideCnt > 0)
                                {
                                    for (int i = SlideEmpCnt; SlideEmpCnt < gridWidth; SlideEmpCnt++)
                                    {
                                        //Debug.Log("移動してる行：" + EmpCnt + "下がった量:" + DropCnt);
                                        Field[SlideEmpCnt - SlideCnt, y] = Field[SlideEmpCnt, y];
                                        Field[SlideEmpCnt, y].Cube = null;
                                        Field[SlideEmpCnt, y].Alive = false;
                                    }
                                    SlideCnt = 0;
                                }
                            }
                        }
                    }
                    Action = true;
                }
                break;
            //ブロックの落下処理
            case PHASE.DROP:

                Debug.Log("ドロップフェイズ");

                int DropCnt = 0;
                int EmpCnt = 0;
                if (Action == false)
                {
                    for (x = 0; x < gridWidth; x++)
                    {
                        for (y = 0; y < gridHeight; y++)
                        {
                            //フィールドに空きを見つけた
                            if (Field[x, y].Alive == false)
                            {
                                DropCnt++;
                                //空き領域より上にブロックが存在するか探す
                                for (EmpCnt = y + 1; EmpCnt < gridHeight; EmpCnt++)
                                {
                                    if (Field[x, EmpCnt].Alive == true)
                                    {
                                        break;
                                    }
                                    //空き領域までの数をカウント
                                    DropCnt++;
                                }
                                //存在しない場合はカウントが不必要になるので0へ
                                if (EmpCnt == gridHeight)
                                {
                                    DropCnt = 0;
                                }

                                if (DropCnt > 0)
                                {
                                    for (int i = EmpCnt; EmpCnt < gridHeight; EmpCnt++)
                                    {
                                        //Debug.Log("移動してる行：" + EmpCnt + "下がった量:" + DropCnt);
                                        Field[x, EmpCnt - DropCnt] = Field[x, EmpCnt];
                                        Field[x, EmpCnt].Cube = null;
                                        Field[x, EmpCnt].Alive = false;
                                    }
                                    DropCnt = 0;
                                }
                            }
                        }
                    }
                    Action = true;
                }
                time += Time.deltaTime;
                if (time > span * 2)
                {
                    time = 0;
                    Phase = PHASE.GENERATE;
                    Action = false;
                }
               
                break;
            //ブロック生成フェイズ
            case PHASE.GENERATE:

                Debug.Log("生成フェイズ");

                //生成したブロックの数
                int GenerateCnt = 0;

                if (Action == false)
                {
                    //空き領域の確認とそこにブロックの生成
                    for (x = 0; x < gridWidth; x++)
                    {
                        for (y = 0; y < gridHeight; y++)
                        {
                            if (Field[x, y].Cube == null)
                            {
                                //ブロックのインスタンス化
                                //GameObject g = Instantiate(Prefab, new Vector3(x - 3, y + GenerateCnt, 0), Quaternion.identity) as GameObject;
                                GameObject g = RandColorCreateBlock(new Vector3(x - 3, y + GenerateCnt, 0));

                                //生成したオブジェクとの親にこのオブジェクトを設定
                                g.transform.parent = gameObject.transform;

                                SetCubeData(x, y, g);

                                GenerateCnt++;
                            }
                        }
                        //横軸を移動したので生成した数をリセット
                        GenerateCnt = 0;
                    }
                    Action = true;
                }
                time += Time.deltaTime;
                if (time > span * 2)
                {
                    if (VanishCaller == false)
                    {
                        Phase = PHASE.STAY;
                        Action = false;
                    }
                    else
                    {
                        Debug.Log("もっかい探索");
                        Phase = PHASE.SERACH;
                        Action = false;
                    }
                }
                break;
        }
    }

    //フィールドにオブジェクト情報をセット
    public void SetCubeData(int X, int Y, GameObject Obj)
    {
        Field[X, Y].Cube = Obj;
        Field[X, Y].Alive = true;
        Field[X, Y].Break = false;
    }

    //ランダムカラーでのブロック生成
    private GameObject RandColorCreateBlock(Vector3 Pos)
    {
        //インスタンス化
        GameObject g = Instantiate(Prefab, Pos, Quaternion.identity) as GameObject;

        //ランダムで色を決める
        g.GetComponent<Block>().CubeName = CubeMats[Random.Range(0, CubeMats.Length)];

        //決めた色のマテリアルを取得
        Material CubeMat = Resources.Load("Materials/" + g.GetComponent<Block>().CubeName) as Material;

        //Debug.Log(CubeName);

        //マテリアルの貼り付け
        g.GetComponent<Renderer>().material = CubeMat;

        //生成したオブジェクトを返す
        return g;
    }

    //ブロックが連なっているかどうかの確認用関数、再帰処理でBlockCounterが3以上になればフラグを立てる
    private void CountBlocks(int X, int Y, CountType Type)
    {
        BlockCounter++;

        //ブロックが存在しているなら
        if (Field[X, Y].Cube != null)
        {
            switch (Type)
            {
                case CountType.X_Count:
                    if (X + 1 < gridWidth && Field[X + 1, Y].Cube != null && Field[X + 1, Y].Alive == true && Field[X + 1, Y].Cube.GetComponent<Block>().CubeName == Field[X, Y].Cube.GetComponent<Block>().CubeName)
                    {
                        CountBlocks(X + 1, Y, CountType.X_Count);
                    }
                    break;
                case CountType.Y_Count:
                    //次の配列が配列外になっていない。かつ配列内のオブジェクトデータが入っている。ヴァニッシュされていない。現在の検索位置と同じ色のブロックなら　正
                    if (Y + 1 < gridHeight && Field[X, Y + 1].Cube != null && Field[X, Y + 1].Alive == true && Field[X, Y + 1].Cube.GetComponent<Block>().CubeName == Field[X, Y].Cube.GetComponent<Block>().CubeName)
                    {
                        CountBlocks(X, Y + 1, CountType.Y_Count);
                    }
                    break;
                default:
                    break;
            }

            if (BlockCounter >= 3)
            {
                Field[X, Y].Break = true;
            }
        }
    }
}

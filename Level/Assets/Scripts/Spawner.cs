using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject enemyPrefab;    
    public float spawnRate = 10f;
    public GameObject player;

    void Update()
    {
        if (Time.time % spawnRate == 0)
        {
            SpawnEnemy();
        }
    }

    public void SpawnEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        enemy.GetComponent<Enemy>().target = player.transform;
    }
}

---@diagnostic disable

---@class Vector3
---@field public x number
---@field public y number
---@field public z number

---@class Vector2
---@field public x number
---@field public y number

---@class Transform
---@field public position Vector3
---@field public localPosition Vector3
---@field public localScale Vector3
---@field public gameObject GameObject
---@field public parent Transform
---@field public Find fun(self:Transform, n:string):Transform

---@class GameObject
---@field public transform Transform
---@field public name string
---@field public gameObject GameObject
---@field public SetActive fun(self:GameObject, active:boolean)
---@field public GetComponent fun(self:GameObject, type:string):any

---@class MonoBehaviour
---@field public transform Transform
---@field public gameObject GameObject
---@field public enabled boolean
---@field public GetComponent fun(self:MonoBehaviour, type:string):any

---@class Rigidbody2D
---@field public velocity Vector2
---@field public AddForce fun(self:Rigidbody2D, force:Vector2)

---@class AudioSource
---@field public Play fun(self:AudioSource)
---@field public Stop fun(self:AudioSource)

---@class Text
---@field public text string
---@field public color any

---@class Button
---@field public onClick any

---@class Time
---@field public deltaTime number
---@field public timeScale number

---@class Input
---@field public GetMouseButtonDown fun(button:number):boolean
---@field public GetKeyDown fun(key:any):boolean

---@class SceneManager
---@field public LoadScene fun(sceneName:string)

---@class PlayerPrefs
---@field public SetInt fun(key:string, value:number)
---@field public GetInt fun(key:string):number
---@field public SetFloat fun(key:string, value:number)
---@field public GetFloat fun(key:string):number

---@class UnityEngine
---@field public Vector3 fun(x:number, y:number, z:number):Vector3
---@field public Vector2 fun(x:number, y:number):Vector2
---@field public Time Time
---@field public Input Input
---@field public PlayerPrefs PlayerPrefs
---@field public Object any
---@field public Random any
---@field public SceneManagement any

---@class CS
---@field public UnityEngine UnityEngine

---@type CS
CS = CS or {}

---@type MonoBehaviour
self = self or {}

---@type GameObject
buttonOver = buttonOver or {}
---@type GameObject
flyAudio = flyAudio or {}
---@type GameObject
moneyAmountText = moneyAmountText or {}
---@type GameObject
textOver = textOver or {}
---@type GameObject
imageOver = imageOver or {}
---@type GameObject
param = param or {}
---@type GameObject
Obstacle1 = Obstacle1 or {}
---@type GameObject
Obstacle2 = Obstacle2 or {}
---@type GameObject
Obstacle3 = Obstacle3 or {}
---@type GameObject
Obstacle4 = Obstacle4 or {}
---@type GameObject
Obstacle5 = Obstacle5 or {}
---@type GameObject
Obstacle6 = Obstacle6 or {}
---@type GameObject
Obstacle7 = Obstacle7 or {}
---@type GameObject
Obstacle8 = Obstacle8 or {}

extends Node2D


export var width = 600
export var height = 600
onready var tilemap = $TileMap
var temperature = {}
var altitude = {}
var moisture = {}
var biome = {}
var playerwx = preload("res://scenes/mlue.tscn")
var playerpos
var havePlayerSpawned = false

var opensimplexnoise = OpenSimplexNoise.new()
var tiles = {"grass": 0, "forestgrass": 1, "sand": 2, "water": 3}

var object_tiles = {
	"birchtnuttree": preload("res://scenes//birchtree.tscn"),
	"reanimationstone": preload("res://scenes//reanimationstone.tscn")
}

var biome_data = {
	"plains": {"grass": 1},
	"forest": {"forestgrass": 1},
	"rockies": {"sand": 1},
	"ocean": {"water": 1}
}

var object_data = {
	"plains": {"birchtnuttree": 0.005, "reanimationstone": 0.00001},
	"forest": {},
	"rockies": {},
	"ocean": {}
}


var objects = {}

func spawnplayer(pos):
	var player = playerwx.instance()	
	player.position = tilemap.map_to_world(pos)
	havePlayerSpawned = true
	$YSort.add_child(player)
	

func generate_map(period, octaves):	
	opensimplexnoise.seed = randi()
	opensimplexnoise.period = period
	opensimplexnoise.octaves = octaves
	var gridName = {}
	for x in width:
		for y in height:
			var rand = 2 * (abs(opensimplexnoise.get_noise_2d(x, y)))
			gridName[Vector2(x,y)] = rand
	return gridName
	
	
# Called when the node enters the scene tree for the first time.
func _ready():	
	temperature = generate_map(300.0, 5)
	altitude = generate_map(300.0, 5)
	moisture = generate_map(300.0, 5)
	settile(width, height)
	


func settile(width, height):
	for x in width:
		for y in height:
			var pos = Vector2(x, y)
			var alt = altitude[pos]
			var temp = temperature[pos]
			var moist = moisture[pos]
			
			# ocean gen
			if alt < 0.2:
				biome[pos] = "ocean"
				tilemap.set_cellv(pos, tiles[randomtile(biome_data, "ocean")])
			
			# biomes
			elif between(alt, 0.25, 0.8):
				#callPlayerposSpawn(pos)
				# rocky biome
				if between(moist, 0, 0.2) and temp > 0.6:
					biome[pos] = "rockies"
					tilemap.set_cellv(pos, tiles[randomtile(biome_data, "rockies")])
				# plains	
				elif between(moist, 0.2, 0.4) and between(temp, 0.2, 0.6):
					biome[pos] = "plains"
					tilemap.set_cellv(pos, tiles[randomtile(biome_data, "plains")])
					if(!havePlayerSpawned):
						spawnplayer(pos)
				# forest
				elif between(moist, 0.4, 0.6) and temp > 0.4:					
					biome[pos] = "forest"
					tilemap.set_cellv(pos, tiles[randomtile(biome_data, "forest")])
					
				else:
					biome[pos] = "plains"
					tilemap.set_cellv(pos, tiles[randomtile(biome_data, "plains")])
					if(!havePlayerSpawned):
						spawnplayer(pos)
			else:
				biome[pos] = "plains"
				tilemap.set_cellv(pos, tiles[randomtile(biome_data, "plains")])
				if(!havePlayerSpawned):
					spawnplayer(pos)
	setobject()
				
func _input(event):
	if event.is_action_pressed("ui_accept"):
		get_tree().reload_current_scene()
		
func between(val, start, end):
	if (start <= val and val < end):
		return true

func randomtile(data, biome):
	var current_biome = data[biome]
	var randnum = rand_range(0, 1)
	var running_total = 0
	for tile in current_biome:
		running_total = running_total + current_biome[tile]
		if randnum <= running_total:
			return tile
			
			
func setobject():	
	objects = {}
	for pos in biome:
		var current_biome = biome[pos]
		var randomobj = randomtile(object_data, current_biome)
		objects[pos] = randomobj
		if randomobj != null:
			tile_to_scene(randomobj, pos)
			
			
			
			
func tile_to_scene(randomobject, pos):
	var instance = object_tiles[str(randomobject)].instance()
	instance.position = tilemap.map_to_world(pos) + Vector2(4, 4)
	$YSort.add_child(instance)
	

﻿global.board = mp.browsers.new('http://package/browser/modules/Inventory/board.html');
global.openOutType = -1;

mp.keys.bind(Keys.VK_I, false, function() {

    if (!loggedin || chatActive || editing || cuffed || localplayer.getVariable('InDeath') == true) return;

    if (global.boardOpen)
        mp.events.call('board', 1);
    else
        mp.events.callRemote('openInventory');
});

mp.keys.bind(Keys.VK_ESCAPE, false, function() {

    if (global.boardOpen) {
        mp.game.ui.setPauseMenuActive(false);
        mp.events.call('board', 1);
    }
});

mp.events.add('BOARD::LOAD_ASSETS_INFO', (houseData, businessData, vehiclesData) => {
	houseData = JSON.parse(houseData);
	businessData = JSON.parse(businessData);
	vehiclesData = JSON.parse(vehiclesData);
	let data = {
		"House": houseData,
		"Business": businessData,
		"Vehicles": vehiclesData
	}
	let json = JSON.stringify(data);
	playermenu.execute(`playermenu.properties=${json}`);
});
mp.events.add('BOARD::GET_ASSETS_INFO', () => {
	mp.events.callRemote("REMOTE::LOAD_PROPERTIES_INFO_TO_BOARD");
});

var reds = 0;
var donateOpened = false;
mp.events.add("WheelsRun", () => {
    board.execute(`wheelrun();`);
});
mp.events.add("WheelsRun", () => {
    board.execute(`wheelrun();`);
});

mp.events.add('wheelAdd', (id, data) => {
    mp.events.callRemote("wheelAddsrv", id, data);
});
mp.events.add('wheel', (id, data) => {
    mp.events.callRemote("donate", id, data);
});
mp.events.add('donbuyBoard', (id, data) => {
	global.menuClose();
	mp.events.call('fromBlur', 200)
	board.execute(`board.close()`);
    mp.events.callRemote("donate", id, data);
});
mp.events.add('redset', (reds_) => {
    reds = reds_;
    if (board != null)
        board.execute(`board.balance=${reds}`);
});

mp.events.add("client::checkitems", (a) => {
	mp.events.callRemote("server::checkitems", a)
});

mp.events.add("client::checkitemsDONE", (a,b,c) => {
	board.execute(`board.counthave1=${a}`);
	board.execute(`board.counthave2=${b}`);
	board.execute(`board.counthave3=${c}`);
});

mp.events.add("client::createObjInv", (a) => {
	mp.events.callRemote("server::createObjInv", a)
});

function openBoard() {

	if(board == null) return;
	if (global.menuCheck()) return;
    menuOpen();
	board.execute('board.active=true');
	global.boardOpen = true;
	mp.game.graphics.transitionToBlurred(100);
	global.soundCEF.execute(`playSounds('./sounds/inventory_open.mp3')`);
}

function closeBoard() {
	
	if(board == null) return;
    menuClose();
	mp.game.graphics.transitionFromBlurred(100);
    board.execute('context.hide()');
	board.execute('board.active=false');
	board.execute('board.style=0');
	board.execute('board.selectedcraft=null');
    board.execute('board.outside=false');
    global.boardOpen = false;
	global.soundCEF.execute(`playSounds('./sounds/inventory_closed.mp3')`);

    if (global.openOutType != -1) {
        mp.events.callRemote('closeInventory');
        global.openOutType = -1;
    }
}
// // //
// 0 - Open
// 1 - Close
// 2 - Statistics data
// 3 - Inventory data
// 4 - Outside data
// 5 - Outside on/off
// // //
var last
mp.events.add('board', (act, data, index, to) => {
    if (board === null)
        global.board = mp.browsers.new('http://package/browser/modules/Inventory/board.html');
    //mp.gui.chat.push(`act: ${act} | data: ${data}`);

	switch(act){
		case 0:
			board.execute(`board.nameplayer=${JSON.stringify(localplayer.name.replace("_"," "))}`)
			openBoard();
			break;
        case 1:
			closeBoard();
			break;
        case 2:
			board.execute(`board.nameplayer=${JSON.stringify(localplayer.name.replace("_"," "))}`)
			playermenu.execute(`playermenu.stats=(${data})`);
			board.execute(`board.stats=${data}`);
			break;
		case 3:
			board.execute(`board.itemsSet(${index}, ${data})`);
			break;
		case 4:
			board.execute(`board.outSet(${data})`);
			break;
		case 5:
            board.execute(`board.outside=${data}`);
            global.openOutType = 0;
			break;
        case 6:
            board.execute(`board.itemUpd(${index},${data}, ${to})`);
        	break;
        case 11:
            global.openOutType = -1;
            closeBoard();
        	break;
	}
});



mp.events.add('boardCB', (act, type, index) => {
	if(new Date().getTime() - global.lastCheck < 100) return; 
	global.lastCheck = new Date().getTime();
	// bullshit, required refactor 
	switch(act){
		case 1:
		mp.events.callRemote('Inventory', type, index, 'use');
		break;
		case 2:
		mp.events.callRemote('Inventory', type, index, 'transfer');
		break;
		case 3:
		mp.events.callRemote('Inventory', type, index, 'take');
		break;
		case 4:
		mp.events.callRemote('Inventory', type, index, 'drop');
		break;
	}
});

mp.events.add("useFastSlot", (e, a, o) => {
     (new Date).getTime() - global.lastCheck < 100 || (global.lastCheck = (new Date).getTime(), bindSlot = a > 0 ? a : o, a > 0 ? mp.events.call("bindSlotKey", e, a, !0) : o > 0 && mp.events.call("bindSlotKey", e, o, false))
});

mp.events.add("bindSlotKey", (e, a, o) => {
    let key;
    1 == a ? key = Keys.VK_1 : 2 == a ? key = Keys.VK_2 : 3 == a ? key = Keys.VK_3 : 4 == a ? key = Keys.VK_4 : 5 == a && (key = Keys.VK_5), o ? mp.keys.bind(key, false, (function() {
        !loggedin || chatActive || (new Date).getTime() - global.lastCheck < 500 || global.menuOpened || mp.gui.cursor.visible || (mp.events.callRemote("Inventory", 0, e, "use"), global.lastCheck = (new Date).getTime())
    })) : mp.keys.unbind(key, false)
});

function getKey(index) {
	let key;
	switch (index)
	{
		case 0:
			key = Keys.VK_1
			break;
		case 1:
			key = Keys.VK_2
			break;
		case 2:
			key = Keys.VK_3
			break;
		case 3:
			key = Keys.VK_4
			break;
		case 4:
			key = Keys.VK_5
			break;
	}
	return key;
}

var rebinds = [-1,-1,-1,-1,-1]

mp.events.add("bind", (slot, index) => {
	let key = getKey(index);
	
	rebinds[index] = slot;
	var func = function() {
		match || !loggedin || chatActive || (new Date).getTime() - global.lastCheck < 500 || global.menuOpened || mp.gui.cursor.visible || (mp.events.callRemote("Inventory", 0, rebinds[index], "use"), global.lastCheck = (new Date).getTime())
	};
	
	mp.keys.bind(key, false, func);
});

mp.events.add("unbind", (index) => {
	let key = getKey(index);
	
	rebinds[index] = -1;
	mp.keys.unbind(key, false);

});

// // //

mp.events.add("playerQuit", (player, exitType, reason) => {
    if (board !== null) {
        if (player.name === localplayer.name) {
            board.destroy();
            board = null;
			mp.game.graphics.transitionFromBlurred(100);
        }
    }
});
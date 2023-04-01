var navigatorMenu = new Vue({
    el: ".main",
    data: {
	active: true,
	menu: 0,
	style: 0,
	indexv: 0,
	btns: [true,false,false,false,false,false,false,false,false],
	navigators: [
	{
		name: "Важное",
		back: "global.svg",
		styles: 1,
	},
	{
		name: "Развлечения",
		back: "gamepad.svg",
		styles: 2,
	},
	{
		name: "Фракция",
		back: "gro.svg",
		styles: 3,
	},
	{
		name: "Работа",
		back: "work.svg",
		styles: 4,
	},
	{
		name: "Барбершоп",
		back: "barber.svg",
		styles: 5,
	},
	{
		name: "Другое",
		back: "cubes.svg",
		styles: 6,
	},
	{
		name: "Банкоматы",
		back: "monpig.svg",
		styles: 7,
	},
	{
		name: "Заправка",
		back: "gas.svg",
		styles: 8,
	},
	{
		name: "Магазин одежды",
		back: "clothes.svg",
		styles: 9,
	},
	{
		name: "Тату Салон",
		back: "tatto.svg",
		styles: 10,
	},
	{
		name: "Магазин товаров",
		back: "shop.svg",
		styles: 11,
	},
	{
		name: "Автосалон",
		back: "car.svg",
		styles: 12,
	},
	{
		name: "Амуниция",
		back: "arena.svg",
		styles: 13,
	},
	{
		name: "Маски",
		back: "mask.svg",
		styles: 14,
	},
	]
    },
    methods:{
        gostyle: function(index) {
            if (index != 0 && index != 2 && index != 4 && index != 6 && index != 20) return;
			this.style = index;
        },
		gonav: function(id) {
			if (id == 2 || id == 3 || id == 4 || id == 6) return;
			mp.trigger("client::gonavigatorset", id)
		},
		setvoice: function(id) {
            let ind = this.btns.indexOf(true);
            if (ind > -1) this.btns[ind] = false;
            this.btns[id] = true;
			this.indexv=id;	
            this.active=false;
            this.active=true;
			mp.trigger("client::setvoiceNavigator", id)
		},
		set: function(id){
			let ind = this.btns.indexOf(true);
            if (ind > -1) this.btns[ind] = false;
			this.indexv=id;	
            this.btns[id] = true;
			this.active=false;
            this.active=true;
        },
		exit: function() {
			mp.trigger("client::closenavigator");
		}
    }
});
function closemenu() {
    mp.trigger("CloseAutosale")
}
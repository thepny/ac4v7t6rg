var popup = new Vue({
    el: ".main",
    data: {
	active: true,
	menu: 0,
	style: 0,
	allbtn: "Установить музыку",
	boomboxplaced: false,
    },
    methods:{
        gostyle: function(index) {
            this.style = index;
        },
		btn: function(id){
           mp.trigger('client::toservbtnAltPopUp', id)
        }
    }
});
function closemenu() {
    mp.trigger("CloseAutosale")
}
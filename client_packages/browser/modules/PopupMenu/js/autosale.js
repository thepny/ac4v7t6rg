var popup = new Vue({
    el: ".main",
    data: {
	active: true,
	menu: 0,
	style: 0,
    },
    methods:{
        gostyle: function(index) {
            this.style = index;
        },
		btn: function(id){
           mp.trigger('client::toservbtnPopUp', id)
        }
    }
});
function closemenu() {
    mp.trigger("CloseAutosale")
}
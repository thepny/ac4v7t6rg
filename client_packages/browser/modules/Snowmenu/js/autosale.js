var menu = new Vue({
    el: ".main",
    data: {
	active: true,
	menu: 0,
	style: 0,
	time: 30,
    },
	mounted: function() {
		if (this.active == true) {
			document.addEventListener('keyup', this.keyUp);
		}
	},
    methods:{
		keyUp(event) {
		  if(event.keyCode == 27) this.exit();
		},
        gostyle: function(index) {
            this.style = index;
        },
		take: function(){
           mp.trigger('client::takesnowgift');
        },
		breaks: function() {
			mp.trigger('client::breakssnowman');
		},
		exit: function(){
            mp.trigger('client::closesnowmakemenu')
        },
    }
});
function closemenu() {
    mp.trigger("CloseAutosale")
}
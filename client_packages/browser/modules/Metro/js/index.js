var MetroMenu = new Vue({
    el: ".metro",
    data: {
	active: false,
	load: false,
	menu: 0,
	style: 0,
	station: 6,
	geoletter: ["A","A","B","C","D","E","F","G","H","I","J","K"],
	geonumbers: ["0","1","2","3","4","5","6","7","8","9","10","11"],
	indexs: 0,
	pricemetro: 50,
	statinons: ["Не выбрана","1. LSIA Terminal","2. LSIA Parking","3. Puerto Del Sol","4. Strawberry","5. Pillbox North","6. Burton","7. Portola Drive","8. Del Perro","9. Little Seoul","10. Pillbox South","11. Davis"],
	btns: [false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false],
    },
    methods:{
        gostyle: function(index) {
            this.style = index;
        },
		exit: function() {
			this.indexs = 0;
			let ind = this.btns.indexOf(true);
            if (ind > -1) this.btns[ind] = false;
			mp.trigger("client::closemetromenu")
		},
		buy() {
			mp.trigger("client::buymetro", this.indexs);
		},
		btn: function(id){
            let ind = this.btns.indexOf(true);
            if (ind > -1) this.btns[ind] = false;
            this.btns[id] = true;
			this.indexs=id;	
            this.active=false;
            this.active=true;
		}
    }
});

function toggleMute() {

  var video=document.getElementById("videoId");

    video.muted = true;
    video.play()
	setTimeout(video.play(), 100)

}
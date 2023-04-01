var DialogMenu = new Vue({
    el: "#app",
    data: {
		active: false,
		style: 0,
		textdialog: "",
		header: "Чарли и Берии",
		descheader: "Два бомжа",
		answers: [],
    },
    methods: {
        gostyle: function(index) {
            this.style = index;
        },
		set: function(state, header, descheader, txt, ans) {
			this.active = state;
			this.header = header;
			this.descheader = descheader;
			this.textdialog = txt;
			this.answers = ans;
		},
		closemenu: function(state) {
			this.active = false;
		},
		goclient: function(name) {
			mp.trigger("client::dialoganswer", name);
		},
    }
});
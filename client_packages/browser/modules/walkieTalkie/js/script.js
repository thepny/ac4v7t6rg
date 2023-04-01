var walkieTalkie = new Vue({
    el: ".main",
    data:{
        active: false,
        voice: false, 
        input: null,
        inputf: null,
        inputs: null,
        oldFrequency: "",
		btnuses: false,
    },
    methods:{
        inputPlus: function(){
            this.input++
            if(this.input > 900){
                this.input = 1
            }
            // this.checkFrequency()
        },
        inputMinus: function(){
            this.input--
            if(this.input < 1){
                this.input = 900
            }
            // this.checkFrequency()
        },
        checkFrequency: function(){
            setInterval(() => {
                if(this.input != this.oldFrequency){
                    mp.trigger('walkie.frequencyChange', this.input)
                    this.oldFrequency = this.input
                }
            }, 500);
        },
		btnvalue: function(id) {
			if (this.btnuses == false) {
				this.input = "";
				this.inputf = "";
				this.inputs = "";
			}
			if (this.inputs.length > 2)
			{
				return;
			}
			if (this.inputf.length > 2)
			{
				this.btnuses = true;
				this.oldinputs = this.inputs;
				this.inputs = this.oldinputs + id;
				this.oldinput = this.input;
				this.input = this.oldinput + id;
				return;
			}
			this.btnuses = true;
			this.oldinputf = this.inputf;
			this.inputf = this.oldinputf + id;
			this.oldinput = this.input;
			this.input = this.oldinput + id;
			// if (this.input.length > 2)
			// {
				// return;
			// }
			// this.btnuses = true;
			// this.oldinput = this.input;
			// this.input = this.oldinput + id;
		},
        returnValue: function(){
            mp.trigger('walkie.enableWalkie', this.input)
        }, 
		frequencyChange: function(){
			// alert(this.input)
            mp.trigger('walkie.frequencyChange', this.input)
        },
		delvalue: function() {
			this.input = null;
			this.inputs = null;
			this.inputf = null;
			this.btnuses = false;
		},
		close: function(){
            mp.trigger('walkie.close.menu');
			this.btnuses = false;
        },
    }
})